using Microsoft.EntityFrameworkCore.Storage;
using OrdersService.Models;

namespace OrdersService.Interfaces;

public interface IOrderRepository
{
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task<Order> CreateOrderAsync(Order order);
    Task<List<Order>> GetOrdersByUserIdAsync(Guid userId);
    Task<Order?> GetOrderByIdAsync(Guid orderId, Guid userId);
    Task<Order?> GetOrderByIdAsync(Guid orderId);
    Task UpdateOrderAsync(Order order);
    Task<List<OutboxMessage>> GetUnsentOutboxMessagesAsync(int limit = 100);
    Task<OutboxMessage> CreateOutboxMessageAsync(OutboxMessage message);
    Task UpdateOutboxMessageAsync(OutboxMessage message);
}

