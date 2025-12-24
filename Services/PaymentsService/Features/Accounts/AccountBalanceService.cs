using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentsService.Abstractions.Interfaces;
using PaymentsService.Domain.Entities;
using PaymentsService.Infrastructure.Data;

namespace PaymentsService.Features.Accounts;

public sealed class AccountBalanceService(PaymentsDbContext dbContext, ILogger<AccountBalanceService> logger) : IAccountBalanceService
{
    private const int MaxRetry = 3;

    public async Task<AccountBalance> GetBalanceAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var balance = await dbContext.AccountBalances.FirstOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);
        if (balance is null)
        {
            balance = new AccountBalance { AccountId = accountId, Balance = 0m };
            dbContext.AccountBalances.Add(balance);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return balance;
    }

    public async Task<AccountBalance> TopUpAsync(Guid accountId, decimal amount, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxRetry; attempt++)
        {
            var balance = await GetBalanceAsync(accountId, cancellationToken);
            balance.Balance += amount;

            dbContext.AccountTransactions.Add(new AccountTransaction
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Delta = amount,
                CreatedAtUtc = DateTime.UtcNow
            });

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return balance;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency when topping up account {AccountId}, retry {Attempt}", accountId, attempt);
                dbContext.ChangeTracker.Clear();
            }
        }

        throw new InvalidOperationException("Unable to top up balance after retries");
    }

    public async Task<(bool Success, string? Error)> TryDebitAsync(Guid accountId, Guid orderId, decimal amount, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxRetry; attempt++)
        {
            var existing = await dbContext.AccountTransactions.FirstOrDefaultAsync(t => t.OrderId == orderId, cancellationToken);
            if (existing is not null)
            {
                return (true, null);
            }

            var balance = await GetBalanceAsync(accountId, cancellationToken);
            if (balance.Balance < amount)
            {
                return (false, "Insufficient funds");
            }

            balance.Balance -= amount;

            dbContext.AccountTransactions.Add(new AccountTransaction
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                OrderId = orderId,
                Delta = -amount,
                CreatedAtUtc = DateTime.UtcNow
            });

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return (true, null);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency when debiting account {AccountId} for order {OrderId}, retry {Attempt}", accountId, orderId, attempt);
                dbContext.ChangeTracker.Clear();
            }
        }

        return (false, "Failed to debit after retries");
    }

    public async Task<(bool Success, string? Error)> DebitWithoutSaveAsync(Guid accountId, Guid orderId, decimal amount, CancellationToken cancellationToken)
    {
        var existing = await dbContext.AccountTransactions.FirstOrDefaultAsync(t => t.OrderId == orderId, cancellationToken);
        if (existing is not null)
        {
            return (true, null);
        }

        var balance = await GetBalanceAsync(accountId, cancellationToken);
        if (balance.Balance < amount)
        {
            return (false, "Insufficient funds");
        }

        balance.Balance -= amount;

        dbContext.AccountTransactions.Add(new AccountTransaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            OrderId = orderId,
            Delta = -amount,
            CreatedAtUtc = DateTime.UtcNow
        });

        return (true, null);
    }
}

