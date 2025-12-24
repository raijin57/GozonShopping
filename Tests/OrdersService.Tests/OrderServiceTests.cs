using System.Text.Json;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OrdersService.Abstractions.Interfaces;
using OrdersService.Domain.Entities;
using OrdersService.Features.Orders;
using OrdersService.Features.Orders.Contracts;
using Shared.Contracts.Messages;

namespace OrdersService.Tests.Unit;

public class OrderServiceTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new AutoMoqCustomization());

    [Fact]
    public async Task CreateAsync_Добавляет_заказ_и_сообщение_outbox()
    {
        // Arrange
        var ordersRepo = new Mock<IOrdersRepository>();
        var outboxRepo = new Mock<IOutboxRepository>();
        var logger = new Mock<ILogger<OrderService>>();

        Order? capturedOrder = null;
        OutboxMessage? capturedOutbox = null;

        ordersRepo
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns<Order, CancellationToken>((o, _) =>
            {
                capturedOrder = o;
                return Task.CompletedTask;
            });

        outboxRepo
            .Setup(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Returns<OutboxMessage, CancellationToken>((m, _) =>
            {
                capturedOutbox = m;
                return Task.CompletedTask;
            });

        var service = new OrderService(ordersRepo.Object, outboxRepo.Object, logger.Object);
        var request = _fixture.Create<CreateOrderRequest>();

        // Act
        var result = await service.CreateAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.Amount.Should().Be(request.Amount);
        capturedOrder.UserId.Should().Be(request.UserId);
        capturedOrder.Description.Should().Be(request.Description);

        capturedOutbox.Should().NotBeNull();
        capturedOutbox!.Type.Should().Be(nameof(OrderPaymentRequested));

        var payload = JsonSerializer.Deserialize<OrderPaymentRequested>(capturedOutbox.Payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        payload.Should().NotBeNull();
        payload!.OrderId.Should().Be(capturedOrder.Id);
        payload.UserId.Should().Be(capturedOrder.UserId);
        payload.Amount.Should().Be(capturedOrder.Amount);

        outboxRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
