using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentsService.Abstractions.Interfaces;
using PaymentsService.Domain.Entities;
using PaymentsService.Infrastructure.Data;
using Gozon.Shared;

namespace PaymentsService.Features.Payments;

public sealed class PaymentProcessor(
    PaymentsDbContext dbContext,
    IAccountsRepository accountsRepository,
    IAccountBalanceService balanceService,
    IInboxRepository inboxRepository,
    IOutboxRepository outboxRepository,
    ILogger<PaymentProcessor> logger) : IPaymentProcessor
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task ProcessAsync(OrderPaymentRequested message, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var existingInbox = await inboxRepository.GetByMessageIdAsync(message.MessageId.ToString(), cancellationToken);

                if (existingInbox is not null && existingInbox.ProcessedAtUtc is not null)
                {
                    logger.LogInformation("Payment message {MessageId} already processed", message.MessageId);
                    return;
                }

                if (existingInbox is null)
                {
                    existingInbox = new InboxMessage
                    {
                        Id = Guid.NewGuid(),
                        MessageId = message.MessageId.ToString(),
                        Type = nameof(OrderPaymentRequested),
                        Payload = JsonSerializer.Serialize(message, SerializerOptions),
                        ReceivedAtUtc = DateTime.UtcNow
                    };
                    await inboxRepository.AddAsync(existingInbox, cancellationToken);
                }

                PaymentStatus status;
                string? reason = null;

                var account = await accountsRepository.GetByUserIdAsync(message.UserId, cancellationToken);
                if (account is null)
                {
                    status = PaymentStatus.Failed;
                    reason = "Account not found";
                }
                else
                {
                    var (success, error) = await balanceService.DebitWithoutSaveAsync(account.Id, message.OrderId, message.Amount, cancellationToken);
                    if (!success)
                    {
                        status = PaymentStatus.Failed;
                        reason = error ?? "Debit failed";
                    }
                    else
                    {
                        status = PaymentStatus.Success;
                    }
                }

                var evt = new OrderPaymentStatusChanged(
                    MessageId: Guid.NewGuid(),
                    OrderId: message.OrderId,
                    UserId: message.UserId,
                    Status: status,
                    Reason: reason,
                    OccurredAtUtc: DateTime.UtcNow);

                var outbox = new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = nameof(OrderPaymentStatusChanged),
                    Payload = JsonSerializer.Serialize(evt, SerializerOptions),
                    CreatedAtUtc = DateTime.UtcNow
                };

                await outboxRepository.AddAsync(outbox, cancellationToken);

                existingInbox.ProcessedAtUtc = DateTime.UtcNow;

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                logger.LogInformation("Payment result for order {OrderId}: {Status}", message.OrderId, status);
            }
            catch (DbUpdateConcurrencyException)
            {
                logger.LogWarning("Concurrency exception in PaymentProcessor for message {MessageId}, retrying...", message.MessageId);
                dbContext.ChangeTracker.Clear();
                throw;
            }
        });
    }
}

