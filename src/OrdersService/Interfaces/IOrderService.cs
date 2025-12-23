using OrdersService.DTOs;
using OrdersService.Messages;

namespace OrdersService.Interfaces;

public interface IOrderService
{
    Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request);
    Task<List<OrderResponse>> GetOrdersAsync(Guid userId);
    Task<OrderResponse?> GetOrderAsync(Guid orderId, Guid userId);
    Task ProcessPaymentStatusAsync(string messageType, string messageType2, byte[] body);
    Task ProcessOutboxAsync();
}

