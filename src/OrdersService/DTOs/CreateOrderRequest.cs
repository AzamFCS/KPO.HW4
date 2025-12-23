namespace OrdersService.DTOs;

public class CreateOrderRequest
{
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}

