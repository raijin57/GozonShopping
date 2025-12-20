using Microsoft.AspNetCore.SignalR;

namespace OrdersService.Features.Notifications;

public sealed class OrderStatusHub : Hub
{
    public Task JoinOrder(Guid orderId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, orderId.ToString());
}

