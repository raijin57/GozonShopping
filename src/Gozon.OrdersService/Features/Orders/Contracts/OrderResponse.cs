using OrdersService.Domain.Entities;

namespace OrdersService.Features.Orders.Contracts;

public sealed record OrderResponse(Guid Id, Guid UserId, decimal Amount, string Description, OrderStatus Status, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc)
{
    public static OrderResponse From(Order order) =>
        new(order.Id, order.UserId, order.Amount, order.Description, order.Status, order.CreatedAtUtc, order.UpdatedAtUtc);
}

