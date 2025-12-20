namespace OrdersService.Domain.Entities;

public sealed class OutboxMessage
{
    public Guid Id { get; init; }

    public string Type { get; init; } = string.Empty;

    public string Payload { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;

    public DateTime? ProcessedAtUtc { get; set; }

    public int Attempt { get; set; }

    public string? Error { get; set; }
}

