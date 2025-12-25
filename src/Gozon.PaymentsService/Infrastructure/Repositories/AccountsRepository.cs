using Microsoft.EntityFrameworkCore;
using PaymentsService.Abstractions.Interfaces;
using PaymentsService.Domain.Entities;
using PaymentsService.Infrastructure.Data;

namespace PaymentsService.Infrastructure.Repositories;

public sealed class AccountsRepository(PaymentsDbContext dbContext) : IAccountsRepository
{
    public Task<Account?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Accounts.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Accounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task AddAsync(Account account, CancellationToken cancellationToken) =>
        dbContext.Accounts.AddAsync(account, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

