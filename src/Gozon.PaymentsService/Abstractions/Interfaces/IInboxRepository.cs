using PaymentsService.Domain.Entities;

namespace PaymentsService.Abstractions.Interfaces;

public interface IInboxRepository
{
    Task<InboxMessage?> GetByMessageIdAsync(string messageId, CancellationToken cancellationToken);

    Task AddAsync(InboxMessage message, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

