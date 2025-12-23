using PaymentsService.DTOs;
using PaymentsService.Models;

namespace PaymentsService.Interfaces;

public interface IPaymentService
{
    Task<Account?> CreateAccountAsync(Guid userId);
    Task<bool> TopUpAccountAsync(Guid userId, decimal amount);
    Task<BalanceResponse?> GetBalanceAsync(Guid userId);
    Task<bool> ProcessOrderPaymentAsync(string messageId, string messageType, byte[] body);
    Task ProcessOutboxAsync();
}

