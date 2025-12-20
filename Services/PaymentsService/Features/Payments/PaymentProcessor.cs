using System.Text.Json;
using Microsoft.Extensions.Logging;
using PaymentsService.Abstractions.Interfaces;
using PaymentsService.Domain.Entities;
using Shared.Contracts.Messages;

namespace PaymentsService.Features.Payments;

public sealed class PaymentProcessor(
    IAccountsRepository accountsRepository,
    IAccountBalanceService balanceService,
    IInboxRepository inboxRepository,
    IOutboxRepository outboxRepository,
    ILogger<PaymentProcessor> logger) : IPaymentProcessor
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task ProcessAsync(OrderPaymentRequested message, CancellationToken cancellationToken)
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
            await inboxRepository.SaveChangesAsync(cancellationToken);
        }

        var account = await accountsRepository.GetByUserIdAsync(message.UserId, cancellationToken);
        if (account is null)
        {
            await PublishResult(message, PaymentStatus.Failed, "Account not found", cancellationToken);
            await MarkInboxProcessed(existingInbox, cancellationToken);
            return;
        }

        var debitResult = await balanceService.TryDebitAsync(account.Id, message.OrderId, message.Amount, cancellationToken);
        if (!debitResult.Success)
        {
            await PublishResult(message, PaymentStatus.Failed, debitResult.Error ?? "Debit failed", cancellationToken);
            await MarkInboxProcessed(existingInbox, cancellationToken);
            return;
        }

        await PublishResult(message, PaymentStatus.Success, null, cancellationToken);
        await MarkInboxProcessed(existingInbox, cancellationToken);
    }

    private async Task PublishResult(OrderPaymentRequested original, PaymentStatus status, string? reason, CancellationToken cancellationToken)
    {
        var evt = new OrderPaymentStatusChanged(
            MessageId: Guid.NewGuid(),
            OrderId: original.OrderId,
            UserId: original.UserId,
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
        await outboxRepository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Payment result for order {OrderId}: {Status}", original.OrderId, status);
    }

    private async Task MarkInboxProcessed(InboxMessage inbox, CancellationToken cancellationToken)
    {
        inbox.ProcessedAtUtc = DateTime.UtcNow;
        await inboxRepository.SaveChangesAsync(cancellationToken);
    }
}

