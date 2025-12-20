using Microsoft.EntityFrameworkCore;
using OrdersService.Abstractions.Interfaces;
using OrdersService.Domain.Entities;
using OrdersService.Infrastructure.Data;

namespace OrdersService.Infrastructure.Repositories;

public sealed class OutboxRepository(OrdersDbContext dbContext) : IOutboxRepository
{
    public Task AddAsync(OutboxMessage message, CancellationToken cancellationToken) =>
        dbContext.OutboxMessages.AddAsync(message, cancellationToken).AsTask();

    public async Task<IReadOnlyCollection<OutboxMessage>> GetUnprocessedAsync(int take, CancellationToken cancellationToken)
    {
        var messages = await dbContext.OutboxMessages
            .Where(x => x.ProcessedAtUtc == null)
            .OrderBy(x => x.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

        return messages;
    }

    public Task MarkAsProcessedAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        message.ProcessedAtUtc = DateTime.UtcNow;
        message.Attempt += 1;
        dbContext.OutboxMessages.Update(message);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

