using System.Text;
using System.Text.Json;
using OrdersService.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrdersService.Services;

public class RabbitMQService : IMessageBus, IDisposable
{
    private readonly IConnection? _connection;
    private readonly IModel? _channel;
    private readonly string _orderPaymentQueue = "order_payment_queue";
    private readonly string _paymentStatusExchange = "payment_status_exchange";
    private readonly string _paymentStatusQueue = "payment_status_queue";

    public RabbitMQService(string connectionString)
    {
        var factory = new ConnectionFactory { Uri = new Uri(connectionString) };

        var maxRetries = 30;
        var delay = 3000;
        Exception? lastException = null;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.QueueDeclare(_orderPaymentQueue, durable: true, exclusive: false, autoDelete: false);
                _channel.ExchangeDeclare(_paymentStatusExchange, ExchangeType.Fanout, durable: true);
                _channel.QueueDeclare(_paymentStatusQueue, durable: true, exclusive: false, autoDelete: false);
                _channel.QueueBind(_paymentStatusQueue, _paymentStatusExchange, string.Empty);
                Console.WriteLine($"[OrdersService] Successfully connected to RabbitMQ after {i + 1} attempts");
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (i < maxRetries - 1)
                {
                    Console.WriteLine($"[OrdersService] Failed to connect to RabbitMQ (attempt {i + 1}/{maxRetries}). Retrying in {delay}ms...");
                    System.Threading.Thread.Sleep(delay);
                    delay = Math.Min(delay + 1000, 10000);
                }
            }
        }

        Console.WriteLine($"[OrdersService] Failed to connect to RabbitMQ after {maxRetries} attempts");
        throw lastException ?? new Exception("Failed to connect to RabbitMQ");
    }

    public void PublishOrderPayment(string messageType, string payload)
    {
        if (_channel == null) return;
        var body = Encoding.UTF8.GetBytes(payload);
        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.Type = messageType;
        properties.MessageId = Guid.NewGuid().ToString();

        _channel.BasicPublish(string.Empty, _orderPaymentQueue, properties, body);
    }

    public void ConsumePaymentStatus(Action<string, string, byte[]> handler)
    {
        if (_channel == null) return;
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            var messageType = ea.BasicProperties.Type ?? string.Empty;
            handler(messageType, messageType, ea.Body.ToArray());
        };

        _channel.BasicConsume(_paymentStatusQueue, autoAck: true, consumer);
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _channel?.Dispose();
        _connection?.Dispose();
    }
}

