using PaymentsService.Domain.Entities;

namespace PaymentsService.Abstractions.Interfaces;

public interface IAccountBalanceService
{
    Task<AccountBalance> GetBalanceAsync(Guid accountId, CancellationToken cancellationToken);

    Task<AccountBalance> TopUpAsync(Guid accountId, decimal amount, CancellationToken cancellationToken);

    Task<(bool Success, string? Error)> TryDebitAsync(Guid accountId, Guid orderId, decimal amount, CancellationToken cancellationToken);
}

