namespace PaymentsService.Interfaces;

public interface IMessageBus
{
    void PublishPaymentStatus(string messageType, string payload);
    void ConsumeOrderPayment(Action<string, string, byte[], ulong> handler);
    void AckMessage(ulong deliveryTag);
}

