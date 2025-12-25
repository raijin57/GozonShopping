using System.Text.Json;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentsService.Abstractions.Interfaces;
using PaymentsService.Domain.Entities;
using PaymentsService.Features.Payments;
using PaymentsService.Infrastructure.Data;
using Gozon.Shared;

namespace PaymentsService.Tests.Unit;

public class PaymentProcessorTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new AutoMoqCustomization());

    [Fact]
    public async Task ProcessAsync_Отправляет_fail_если_нет_аккаунта()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        using var dbContext = new PaymentsDbContext(options);

        var accountsRepo = new Mock<IAccountsRepository>();
        accountsRepo.Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var balanceService = new Mock<IAccountBalanceService>();

        var inboxRepo = new Mock<IInboxRepository>();
        var outboxRepo = new Mock<IOutboxRepository>();
        var logger = new Mock<ILogger<PaymentProcessor>>();

        InboxMessage? capturedInbox = null;
        OutboxMessage? capturedOutbox = null;

        inboxRepo
            .Setup(r => r.GetByMessageIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InboxMessage?)null);

        inboxRepo
            .Setup(r => r.AddAsync(It.IsAny<InboxMessage>(), It.IsAny<CancellationToken>()))
            .Returns<InboxMessage, CancellationToken>((m, _) =>
            {
                capturedInbox = m;
                return Task.CompletedTask;
            });

        outboxRepo
            .Setup(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Returns<OutboxMessage, CancellationToken>((m, _) =>
            {
                capturedOutbox = m;
                return Task.CompletedTask;
            });

        var processor = new PaymentProcessor(
            dbContext,
            accountsRepo.Object,
            balanceService.Object,
            inboxRepo.Object,
            outboxRepo.Object,
            logger.Object);

        var message = _fixture.Create<OrderPaymentRequested>();

        // Act
        await processor.ProcessAsync(message, CancellationToken.None);

        // Assert
        capturedOutbox.Should().NotBeNull();
        capturedOutbox!.Type.Should().Be(nameof(OrderPaymentStatusChanged));

        var payload = JsonSerializer.Deserialize<OrderPaymentStatusChanged>(capturedOutbox.Payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        payload.Should().NotBeNull();
        payload!.Status.Should().Be(PaymentStatus.Failed);

        // We can't strictly verify SaveChangesAsync on the context easily without mocking the context itself,
        // but since we passed a real context, we know it was called if no exception was thrown.
        // Also we can verify repositories were called.
        inboxRepo.Verify(r => r.AddAsync(It.IsAny<InboxMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        outboxRepo.Verify(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_Успешно_обрабатывает_платеж_с_достаточными_средствами()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        using var dbContext = new PaymentsDbContext(options);

        var account = _fixture.Create<Account>();
        var accountsRepo = new Mock<IAccountsRepository>();
        accountsRepo.Setup(r => r.GetByUserIdAsync(account.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var balanceService = new Mock<IAccountBalanceService>();
        balanceService.Setup(b => b.DebitWithoutSaveAsync(account.Id, It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));

        var inboxRepo = new Mock<IInboxRepository>();
        var outboxRepo = new Mock<IOutboxRepository>();
        var logger = new Mock<ILogger<PaymentProcessor>>();

        InboxMessage? capturedInbox = null;
        OutboxMessage? capturedOutbox = null;

        inboxRepo
            .Setup(r => r.GetByMessageIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InboxMessage?)null);

        inboxRepo
            .Setup(r => r.AddAsync(It.IsAny<InboxMessage>(), It.IsAny<CancellationToken>()))
            .Returns<InboxMessage, CancellationToken>((m, _) =>
            {
                capturedInbox = m;
                return Task.CompletedTask;
            });

        outboxRepo
            .Setup(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Returns<OutboxMessage, CancellationToken>((m, _) =>
            {
                capturedOutbox = m;
                return Task.CompletedTask;
            });

        var processor = new PaymentProcessor(
            dbContext,
            accountsRepo.Object,
            balanceService.Object,
            inboxRepo.Object,
            outboxRepo.Object,
            logger.Object);

        var message = _fixture.Build<OrderPaymentRequested>()
            .With(x => x.UserId, account.UserId)
            .Create();

        // Act
        await processor.ProcessAsync(message, CancellationToken.None);

        // Assert
        capturedOutbox.Should().NotBeNull();
        capturedOutbox!.Type.Should().Be(nameof(OrderPaymentStatusChanged));

        var payload = JsonSerializer.Deserialize<OrderPaymentStatusChanged>(capturedOutbox.Payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        payload.Should().NotBeNull();
        payload!.Status.Should().Be(PaymentStatus.Success);
        payload.OrderId.Should().Be(message.OrderId);
        payload.UserId.Should().Be(message.UserId);

        inboxRepo.Verify(r => r.AddAsync(It.IsAny<InboxMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        outboxRepo.Verify(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        balanceService.Verify(b => b.DebitWithoutSaveAsync(account.Id, message.OrderId, message.Amount, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_Отправляет_fail_при_недостаточных_средствах()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        using var dbContext = new PaymentsDbContext(options);

        var account = _fixture.Create<Account>();
        var accountsRepo = new Mock<IAccountsRepository>();
        accountsRepo.Setup(r => r.GetByUserIdAsync(account.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var balanceService = new Mock<IAccountBalanceService>();
        balanceService.Setup(b => b.DebitWithoutSaveAsync(account.Id, It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Insufficient funds"));

        var inboxRepo = new Mock<IInboxRepository>();
        var outboxRepo = new Mock<IOutboxRepository>();
        var logger = new Mock<ILogger<PaymentProcessor>>();

        OutboxMessage? capturedOutbox = null;

        inboxRepo
            .Setup(r => r.GetByMessageIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InboxMessage?)null);

        outboxRepo
            .Setup(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Returns<OutboxMessage, CancellationToken>((m, _) =>
            {
                capturedOutbox = m;
                return Task.CompletedTask;
            });

        var processor = new PaymentProcessor(
            dbContext,
            accountsRepo.Object,
            balanceService.Object,
            inboxRepo.Object,
            outboxRepo.Object,
            logger.Object);

        var message = _fixture.Build<OrderPaymentRequested>()
            .With(x => x.UserId, account.UserId)
            .Create();

        // Act
        await processor.ProcessAsync(message, CancellationToken.None);

        // Assert
        capturedOutbox.Should().NotBeNull();
        capturedOutbox!.Type.Should().Be(nameof(OrderPaymentStatusChanged));

        var payload = JsonSerializer.Deserialize<OrderPaymentStatusChanged>(capturedOutbox.Payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        payload.Should().NotBeNull();
        payload!.Status.Should().Be(PaymentStatus.Failed);
        payload.Reason.Should().Be("Insufficient funds");
        payload.OrderId.Should().Be(message.OrderId);
        payload.UserId.Should().Be(message.UserId);
    }

    [Fact]
    public async Task ProcessAsync_Игнорирует_уже_обработанное_сообщение()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        using var dbContext = new PaymentsDbContext(options);

        var accountsRepo = new Mock<IAccountsRepository>();
        var balanceService = new Mock<IAccountBalanceService>();
        var inboxRepo = new Mock<IInboxRepository>();
        var outboxRepo = new Mock<IOutboxRepository>();
        var logger = new Mock<ILogger<PaymentProcessor>>();

        var messageId = _fixture.Create<Guid>();
        var processedInbox = _fixture.Build<InboxMessage>()
            .With(x => x.MessageId, messageId.ToString())
            .With(x => x.ProcessedAtUtc, DateTime.UtcNow)
            .Create();

        inboxRepo.Setup(r => r.GetByMessageIdAsync(messageId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(processedInbox);

        var processor = new PaymentProcessor(
            dbContext,
            accountsRepo.Object,
            balanceService.Object,
            inboxRepo.Object,
            outboxRepo.Object,
            logger.Object);

        var message = _fixture.Build<OrderPaymentRequested>()
            .With(x => x.MessageId, messageId)
            .Create();

        // Act
        await processor.ProcessAsync(message, CancellationToken.None);

        // Assert - ничего не должно быть вызвано
        inboxRepo.Verify(r => r.AddAsync(It.IsAny<InboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        outboxRepo.Verify(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        balanceService.Verify(b => b.DebitWithoutSaveAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
