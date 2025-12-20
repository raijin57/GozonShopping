using OrdersService.Domain.Entities;
using OrdersService.Features.Orders.Contracts;

namespace OrdersService.Abstractions.Interfaces;

public interface IOrderService
{
    Task<Order> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Order>> GetByUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}

