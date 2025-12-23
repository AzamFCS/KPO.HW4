using Microsoft.AspNetCore.SignalR;
using OrdersService.Hubs;
using OrdersService.Interfaces;

namespace OrdersService.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<OrderStatusHub> _hubContext;

    public NotificationService(IHubContext<OrderStatusHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyOrderStatusChangeAsync(Guid orderId, string status)
    {
        await _hubContext.Clients.Group(orderId.ToString()).SendAsync("OrderStatusChanged", new
        {
            OrderId = orderId,
            Status = status
        });
    }
}

