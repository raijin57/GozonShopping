using Microsoft.EntityFrameworkCore;
using PaymentsService.Domain.Entities;

namespace PaymentsService.Infrastructure.Data;

public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountBalance> AccountBalances => Set<AccountBalance>();
    public DbSet<AccountTransaction> AccountTransactions => Set<AccountTransaction>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.UserId).IsUnique();
            builder.Property(x => x.CreatedAtUtc).HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<AccountBalance>(builder =>
        {
            builder.HasKey(x => x.AccountId);
            builder.Property(x => x.Balance).HasColumnType("numeric(18,2)");
            builder.Property(x => x.Version).IsRowVersion();
        });

        modelBuilder.Entity<AccountTransaction>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Delta).HasColumnType("numeric(18,2)");
            builder.Property(x => x.CreatedAtUtc).HasColumnType("timestamp with time zone");
            builder.HasIndex(x => x.OrderId).IsUnique().HasFilter("\"OrderId\" IS NOT NULL");
        });

        modelBuilder.Entity<InboxMessage>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.MessageId).IsUnique();
            builder.Property(x => x.Payload).HasColumnType("jsonb");
            builder.Property(x => x.ReceivedAtUtc).HasColumnType("timestamp with time zone");
            builder.Property(x => x.ProcessedAtUtc).HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Payload).HasColumnType("jsonb");
            builder.Property(x => x.CreatedAtUtc).HasColumnType("timestamp with time zone");
            builder.Property(x => x.ProcessedAtUtc).HasColumnType("timestamp with time zone");
        });
    }
}

