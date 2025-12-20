using OrdersService.Domain.Entities;

namespace OrdersService.Abstractions.Interfaces;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<OutboxMessage>> GetUnprocessedAsync(int take, CancellationToken cancellationToken);

    Task MarkAsProcessedAsync(OutboxMessage message, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

