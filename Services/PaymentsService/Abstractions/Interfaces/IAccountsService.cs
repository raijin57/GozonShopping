using PaymentsService.Domain.Entities;
using PaymentsService.Features.Accounts.Contracts;

namespace PaymentsService.Abstractions.Interfaces;

public interface IAccountsService
{
    Task<Account> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken);

    Task<BalanceResponse?> GetBalanceAsync(Guid accountId, CancellationToken cancellationToken);

    Task<BalanceResponse?> GetBalanceByUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<BalanceResponse?> TopUpAsync(Guid accountId, TopUpRequest request, CancellationToken cancellationToken);
}

