using System.Text.Json;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentsService.Abstractions.Interfaces;
using PaymentsService.Domain.Entities;
using PaymentsService.Features.Payments;
using Shared.Contracts.Messages;

namespace PaymentsService.Tests.Unit;

public class PaymentProcessorTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new AutoMoqCustomization());

    [Fact]
    public async Task ProcessAsync_Отправляет_fail_если_нет_аккаунта()
    {
        // Arrange
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
            accountsRepo.Object,
            balanceService.Object,
            inboxRepo.Object,
            outboxRepo.Object,
            logger.Object);

        var message = new OrderPaymentRequested(
            MessageId: Guid.NewGuid(),
            OrderId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            Amount: 500,
            CreatedAtUtc: DateTime.UtcNow);

        // Act
        await processor.ProcessAsync(message, CancellationToken.None);

        // Assert
        capturedOutbox.Should().NotBeNull();
        capturedOutbox!.Type.Should().Be(nameof(OrderPaymentStatusChanged));

        var payload = JsonSerializer.Deserialize<OrderPaymentStatusChanged>(capturedOutbox.Payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        payload.Should().NotBeNull();
        payload!.Status.Should().Be(PaymentStatus.Failed);

        inboxRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        outboxRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
