using Microsoft.EntityFrameworkCore;
using OrdersService.Domain.Entities;

namespace OrdersService.Infrastructure.Data;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(builder =>
        {
            builder.HasKey(o => o.Id);
            builder.Property(o => o.Amount).HasColumnType("numeric(18,2)");
            builder.Property(o => o.Description).HasMaxLength(512);
            builder.Property(o => o.CreatedAtUtc).HasColumnType("timestamp with time zone");
            builder.Property(o => o.UpdatedAtUtc).HasColumnType("timestamp with time zone");
            builder.Property(o => o.Version).IsRowVersion();

            builder.HasIndex(o => o.UserId);
        });

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Type).HasMaxLength(256);
            builder.Property(x => x.Payload).HasColumnType("jsonb");
            builder.Property(x => x.CreatedAtUtc).HasColumnType("timestamp with time zone");
            builder.Property(x => x.ProcessedAtUtc).HasColumnType("timestamp with time zone");
        });
    }
}

