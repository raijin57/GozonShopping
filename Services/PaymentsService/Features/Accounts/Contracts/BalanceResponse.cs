namespace PaymentsService.Features.Accounts.Contracts;

public sealed record BalanceResponse(Guid AccountId, Guid UserId, decimal Balance);

