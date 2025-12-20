using Shared.Contracts.Messages;

namespace OrdersService.Abstractions.Interfaces;

public interface IPaymentStatusProcessor
{
    Task HandleAsync(OrderPaymentStatusChanged message, CancellationToken cancellationToken);
}

