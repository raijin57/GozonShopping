namespace PaymentsService.Domain.Entities;

public sealed class Account
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}

