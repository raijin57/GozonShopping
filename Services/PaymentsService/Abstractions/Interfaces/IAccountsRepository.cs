using PaymentsService.Domain.Entities;

namespace PaymentsService.Abstractions.Interfaces;

public interface IAccountsRepository
{
    Task<Account?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Account account, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

