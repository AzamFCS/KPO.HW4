namespace PaymentsService.Messages;

public class OrderPaymentRequest
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
}

