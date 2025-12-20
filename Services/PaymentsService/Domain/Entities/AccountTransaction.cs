namespace PaymentsService.Domain.Entities;

public sealed class AccountTransaction
{
    public Guid Id { get; init; }

    public Guid AccountId { get; init; }

    public Guid? OrderId { get; init; }

    public decimal Delta { get; init; }

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}

