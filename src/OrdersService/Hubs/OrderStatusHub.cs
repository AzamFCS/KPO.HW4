using Microsoft.AspNetCore.SignalR;

namespace OrdersService.Hubs;

public class OrderStatusHub : Hub
{
    public async Task SubscribeToOrder(Guid orderId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, orderId.ToString());
    }
}

