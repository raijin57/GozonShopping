namespace PaymentsService.Domain.Entities;

public sealed class InboxMessage
{
    public Guid Id { get; init; }

    public string MessageId { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string Payload { get; init; } = string.Empty;

    public DateTime ReceivedAtUtc { get; init; } = DateTime.UtcNow;

    public DateTime? ProcessedAtUtc { get; set; }

    public string? Error { get; set; }

    public int Attempt { get; set; }
}

