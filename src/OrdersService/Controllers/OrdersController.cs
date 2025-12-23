using Microsoft.AspNetCore.Mvc;
using OrdersService.DTOs;
using OrdersService.Interfaces;

namespace OrdersService.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var order = await _orderService.CreateOrderAsync(request);
        return Ok(order);
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] Guid userId)
    {
        var orders = await _orderService.GetOrdersAsync(userId);
        return Ok(orders);
    }

    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetOrder(Guid orderId, [FromQuery] Guid userId)
    {
        var order = await _orderService.GetOrderAsync(orderId, userId);
        if (order == null)
        {
            return NotFound();
        }
        return Ok(order);
    }
}

