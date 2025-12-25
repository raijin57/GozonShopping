namespace Gozon.Shared;

/// <summary>
/// Command sent from Orders Service to Payments Service requesting payment for an order.
/// </summary>
public sealed record OrderPaymentRequested(
    Guid MessageId,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    DateTime CreatedAtUtc);

