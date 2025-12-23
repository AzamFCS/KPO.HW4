namespace PaymentsService.DTOs;

public class BalanceResponse
{
    public Guid UserId { get; set; }
    public decimal Balance { get; set; }
}

