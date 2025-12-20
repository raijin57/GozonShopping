namespace OrdersService.Domain.Entities;

public enum OrderStatus
{
    New = 0,
    Finished = 1,
    Cancelled = 2
}

public sealed class Order
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public decimal Amount { get; set; }

    public string Description { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.New;

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public uint Version { get; set; }
}

