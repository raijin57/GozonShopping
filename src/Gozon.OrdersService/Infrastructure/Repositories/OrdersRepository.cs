using Microsoft.EntityFrameworkCore;
using OrdersService.Abstractions.Interfaces;
using OrdersService.Domain.Entities;
using OrdersService.Infrastructure.Data;

namespace OrdersService.Infrastructure.Repositories;

public sealed class OrdersRepository(OrdersDbContext dbContext) : IOrdersRepository
{
    public Task AddAsync(Order order, CancellationToken cancellationToken) =>
        dbContext.Orders.AddAsync(order, cancellationToken).AsTask();

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Order>> GetByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var items = await dbContext.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return items;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

