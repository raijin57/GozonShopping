namespace OrdersService.Features.Orders.Contracts;

public sealed record CreateOrderRequest(Guid UserId, decimal Amount, string Description);

