using Shared.Contracts.Messages;

namespace PaymentsService.Abstractions.Interfaces;

public interface IPaymentProcessor
{
    Task ProcessAsync(OrderPaymentRequested message, CancellationToken cancellationToken);
}

