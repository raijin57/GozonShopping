using System.Text.Json;
using Microsoft.Extensions.Logging;
using OrdersService.Abstractions.Interfaces;
using OrdersService.Domain.Entities;
using OrdersService.Features.Orders.Contracts;
using Gozon.Shared;

namespace OrdersService.Features.Orders;

public sealed class OrderService(
    IOrdersRepository ordersRepository,
    IOutboxRepository outboxRepository,
    ILogger<OrderService> logger) : IOrderService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<Order> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            throw new ArgumentException("Amount must be positive", nameof(request.Amount));
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Amount = request.Amount,
            Description = request.Description,
            Status = OrderStatus.New,
            CreatedAtUtc = DateTime.UtcNow
        };

        await ordersRepository.AddAsync(order, cancellationToken);

        var message = new OrderPaymentRequested(
            MessageId: Guid.NewGuid(),
            OrderId: order.Id,
            UserId: order.UserId,
            Amount: order.Amount,
            CreatedAtUtc: DateTime.UtcNow);

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(OrderPaymentRequested),
            Payload = JsonSerializer.Serialize(message, SerializerOptions),
            CreatedAtUtc = DateTime.UtcNow
        };

        await outboxRepository.AddAsync(outboxMessage, cancellationToken);
        await outboxRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Order {OrderId} created with amount {Amount} for user {UserId}", order.Id, order.Amount, order.UserId);
        return order;
    }

    public Task<IReadOnlyCollection<Order>> GetByUserAsync(Guid userId, CancellationToken cancellationToken) =>
        ordersRepository.GetByUserAsync(userId, cancellationToken);

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        ordersRepository.GetByIdAsync(id, cancellationToken);
}

