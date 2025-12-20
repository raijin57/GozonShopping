namespace Shared.Contracts.Messages;

/// <summary>
/// Event emitted by Payments Service to signal the result of a payment attempt.
/// </summary>
public sealed record OrderPaymentStatusChanged(
    Guid MessageId,
    Guid OrderId,
    Guid UserId,
    PaymentStatus Status,
    string? Reason,
    DateTime OccurredAtUtc);

public enum PaymentStatus
{
    Success = 1,
    Failed = 2
}

