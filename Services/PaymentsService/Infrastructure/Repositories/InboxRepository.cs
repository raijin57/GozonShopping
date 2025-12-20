using Microsoft.EntityFrameworkCore;
using PaymentsService.Abstractions.Interfaces;
using PaymentsService.Domain.Entities;
using PaymentsService.Infrastructure.Data;

namespace PaymentsService.Infrastructure.Repositories;

public sealed class InboxRepository(PaymentsDbContext dbContext) : IInboxRepository
{
    public Task<InboxMessage?> GetByMessageIdAsync(string messageId, CancellationToken cancellationToken) =>
        dbContext.InboxMessages.FirstOrDefaultAsync(x => x.MessageId == messageId, cancellationToken);

    public Task AddAsync(InboxMessage message, CancellationToken cancellationToken) =>
        dbContext.InboxMessages.AddAsync(message, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

