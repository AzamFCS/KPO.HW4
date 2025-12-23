using Microsoft.EntityFrameworkCore.Storage;
using PaymentsService.Models;

namespace PaymentsService.Interfaces;

public interface IPaymentRepository
{
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task<Account?> GetAccountByUserIdAsync(Guid userId);
    Task<Account> CreateAccountAsync(Account account);
    Task UpdateAccountAsync(Account account);
    Task<PaymentTransaction?> GetPaymentTransactionAsync(Guid orderId, Guid userId);
    Task<PaymentTransaction> CreatePaymentTransactionAsync(PaymentTransaction transaction);
    Task<InboxMessage?> GetInboxMessageByMessageIdAsync(string messageId);
    Task<InboxMessage> CreateInboxMessageAsync(InboxMessage message);
    Task UpdateInboxMessageAsync(InboxMessage message);
    Task<List<OutboxMessage>> GetUnsentOutboxMessagesAsync(int limit = 100);
    Task<OutboxMessage> CreateOutboxMessageAsync(OutboxMessage message);
    Task UpdateOutboxMessageAsync(OutboxMessage message);
}

