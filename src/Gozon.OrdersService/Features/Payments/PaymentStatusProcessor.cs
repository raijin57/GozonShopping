using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using OrdersService.Abstractions.Interfaces;
using OrdersService.Features.Notifications;
using Gozon.Shared;

namespace OrdersService.Features.Payments;

public sealed class PaymentStatusProcessor(
    IOrdersRepository ordersRepository,
    IHubContext<OrderStatusHub> hubContext,
    ILogger<PaymentStatusProcessor> logger) : IPaymentStatusProcessor
{
    public async Task HandleAsync(OrderPaymentStatusChanged message, CancellationToken cancellationToken)
    {
        var order = await ordersRepository.GetByIdAsync(message.OrderId, cancellationToken);
        if (order is null)
        {
            logger.LogWarning("Order {OrderId} not found for payment status {Status}", message.OrderId, message.Status);
            return;
        }

        var newStatus = message.Status == PaymentStatus.Success
            ? Domain.Entities.OrderStatus.Finished
            : Domain.Entities.OrderStatus.Cancelled;

        if (order.Status != newStatus)
        {
            order.Status = newStatus;
            order.UpdatedAtUtc = DateTime.UtcNow;
            await ordersRepository.SaveChangesAsync(cancellationToken);
        }

        await hubContext.Clients.Group(order.Id.ToString())
            .SendAsync("OrderStatusChanged", new
            {
                order.Id,
                Status = order.Status.ToString()
            }, cancellationToken);

        logger.LogInformation("Order {OrderId} status set to {Status}", order.Id, order.Status);
    }
}

