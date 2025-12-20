namespace PaymentsService.Domain.Entities;

public sealed class AccountBalance
{
    public Guid AccountId { get; init; }

    public decimal Balance { get; set; }

    public uint Version { get; set; }
}

