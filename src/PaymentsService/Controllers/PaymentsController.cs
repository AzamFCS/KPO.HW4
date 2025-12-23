using Microsoft.AspNetCore.Mvc;
using PaymentsService.DTOs;
using PaymentsService.Interfaces;

namespace PaymentsService.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("accounts")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        var account = await _paymentService.CreateAccountAsync(request.UserId);
        if (account == null)
        {
            return Conflict("Account already exists for this user");
        }
        return Ok(account);
    }

    [HttpPost("accounts/topup")]
    public async Task<IActionResult> TopUpAccount([FromBody] TopUpAccountRequest request)
    {
        var result = await _paymentService.TopUpAccountAsync(request.UserId, request.Amount);
        if (!result)
        {
            return NotFound("Account not found");
        }
        return Ok(new { success = true });
    }

    [HttpGet("accounts/{userId}/balance")]
    public async Task<IActionResult> GetBalance(Guid userId)
    {
        var balance = await _paymentService.GetBalanceAsync(userId);
        if (balance == null)
        {
            return NotFound("Account not found");
        }
        return Ok(balance);
    }
}

