namespace OrdersService.Interfaces;

public interface INotificationService
{
    Task NotifyOrderStatusChangeAsync(Guid orderId, string status);
}

