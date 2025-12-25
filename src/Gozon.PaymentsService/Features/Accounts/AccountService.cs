using Microsoft.Extensions.Logging;
using PaymentsService.Abstractions.Interfaces;
using PaymentsService.Domain.Entities;
using PaymentsService.Features.Accounts.Contracts;

namespace PaymentsService.Features.Accounts;

public sealed class AccountService(
    IAccountsRepository accountsRepository,
    IAccountBalanceService balanceService,
    ILogger<AccountService> logger) : IAccountsService
{
    public async Task<Account> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var existing = await accountsRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var account = new Account
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        await accountsRepository.AddAsync(account, cancellationToken);
        await accountsRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created account {AccountId} for user {UserId}", account.Id, account.UserId);
        return account;
    }

    public async Task<BalanceResponse?> GetBalanceAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var account = await accountsRepository.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        var balance = await balanceService.GetBalanceAsync(account.Id, cancellationToken);
        return new BalanceResponse(account.Id, account.UserId, balance.Balance);
    }

    public async Task<BalanceResponse?> GetBalanceByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var account = await accountsRepository.GetByUserIdAsync(userId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        var balance = await balanceService.GetBalanceAsync(account.Id, cancellationToken);
        return new BalanceResponse(account.Id, account.UserId, balance.Balance);
    }

    public async Task<BalanceResponse?> TopUpAsync(Guid accountId, TopUpRequest request, CancellationToken cancellationToken)
    {
        var account = await accountsRepository.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        var balance = await balanceService.TopUpAsync(account.Id, request.Amount, cancellationToken);
        return new BalanceResponse(account.Id, account.UserId, balance.Balance);
    }
}

