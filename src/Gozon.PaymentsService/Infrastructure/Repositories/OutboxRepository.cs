using Microsoft.EntityFrameworkCore;
using PaymentsService.Abstractions.Interfaces;
using PaymentsService.Domain.Entities;
using PaymentsService.Infrastructure.Data;

namespace PaymentsService.Infrastructure.Repositories;

public sealed class OutboxRepository(PaymentsDbContext dbContext) : IOutboxRepository
{
    public Task AddAsync(OutboxMessage message, CancellationToken cancellationToken) =>
        dbContext.OutboxMessages.AddAsync(message, cancellationToken).AsTask();

    public async Task<IReadOnlyCollection<OutboxMessage>> GetUnprocessedAsync(int take, CancellationToken cancellationToken)
    {
        var items = await dbContext.OutboxMessages
            .Where(x => x.ProcessedAtUtc == null)
            .OrderBy(x => x.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
        return items;
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

