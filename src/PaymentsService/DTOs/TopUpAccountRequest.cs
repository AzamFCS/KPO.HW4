namespace PaymentsService.DTOs;

public class TopUpAccountRequest
{
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
}

