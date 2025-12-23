using Microsoft.EntityFrameworkCore;
using OrdersService.Data;
using OrdersService.DTOs;
using OrdersService.Interfaces;
using OrdersService.Messages;
using OrdersService.Models;
using System.Text;
using System.Text.Json;

namespace OrdersService.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;
    private readonly IMessageBus _messageBus;

    public OrderService(IOrderRepository repository, IMessageBus messageBus)
    {
        _repository = repository;
        _messageBus = messageBus;
    }

    public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request)
    {
        using var transaction = await _repository.BeginTransactionAsync();
        try
        {
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Amount = request.Amount,
                Description = request.Description,
                Status = "NEW",
                CreatedAt = DateTime.UtcNow
            };

            await _repository.CreateOrderAsync(order);

            var paymentRequest = new OrderPaymentRequest
            {
                OrderId = order.Id,
                UserId = request.UserId,
                Amount = request.Amount
            };

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                MessageType = "OrderPaymentRequest",
                Payload = JsonSerializer.Serialize(paymentRequest),
                Sent = false,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.CreateOutboxMessageAsync(outboxMessage);
            await transaction.CommitAsync();

            return new OrderResponse
            {
                Id = order.Id,
                UserId = order.UserId,
                Amount = order.Amount,
                Description = order.Description,
                Status = order.Status,
                CreatedAt = order.CreatedAt
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<OrderResponse>> GetOrdersAsync(Guid userId)
    {
        var orders = await _repository.GetOrdersByUserIdAsync(userId);

        return orders.Select(o => new OrderResponse
        {
            Id = o.Id,
            UserId = o.UserId,
            Amount = o.Amount,
            Description = o.Description,
            Status = o.Status,
            CreatedAt = o.CreatedAt
        }).ToList();
    }

    public async Task<OrderResponse?> GetOrderAsync(Guid orderId, Guid userId)
    {
        var order = await _repository.GetOrderByIdAsync(orderId, userId);

        if (order == null)
        {
            return null;
        }

        return new OrderResponse
        {
            Id = order.Id,
            UserId = order.UserId,
            Amount = order.Amount,
            Description = order.Description,
            Status = order.Status,
            CreatedAt = order.CreatedAt
        };
    }

    public async Task ProcessPaymentStatusAsync(string messageType, string messageType2, byte[] body)
    {
        var paymentStatus = JsonSerializer.Deserialize<PaymentStatusEvent>(Encoding.UTF8.GetString(body));
        if (paymentStatus == null)
        {
            return;
        }

        var order = await _repository.GetOrderByIdAsync(paymentStatus.OrderId);
        if (order == null)
        {
            return;
        }

        if (paymentStatus.Status == "success")
        {
            order.Status = "FINISHED";
        }
        else if (paymentStatus.Status == "fail")
        {
            order.Status = "CANCELLED";
        }

        await _repository.UpdateOrderAsync(order);
    }

    public async Task ProcessOutboxAsync()
    {
        var unsentMessages = await _repository.GetUnsentOutboxMessagesAsync(100);

        foreach (var message in unsentMessages)
        {
            try
            {
                _messageBus.PublishOrderPayment(message.MessageType, message.Payload);
                message.Sent = true;
                message.SentAt = DateTime.UtcNow;
                await _repository.UpdateOutboxMessageAsync(message);
            }
            catch
            {
            }
        }
    }
}

