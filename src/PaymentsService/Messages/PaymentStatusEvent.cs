namespace PaymentsService.Messages;

public class PaymentStatusEvent
{
    public Guid OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
}

