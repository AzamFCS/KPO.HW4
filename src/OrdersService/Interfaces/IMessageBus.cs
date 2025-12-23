namespace OrdersService.Interfaces;

public interface IMessageBus
{
    void PublishOrderPayment(string messageType, string payload);
    void ConsumePaymentStatus(Action<string, string, byte[]> handler);
}

