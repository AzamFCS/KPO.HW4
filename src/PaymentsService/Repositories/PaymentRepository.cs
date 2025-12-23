using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PaymentsService.Data;
using PaymentsService.Interfaces;
using PaymentsService.Models;

namespace PaymentsService.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentsDbContext _context;

    public PaymentRepository(PaymentsDbContext context)
    {
        _context = context;
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _context.Database.BeginTransactionAsync();
    }

    public async Task<Account?> GetAccountByUserIdAsync(Guid userId)
    {
        return await _context.Accounts
            .FirstOrDefaultAsync(a => a.UserId == userId);
    }

    public async Task<Account> CreateAccountAsync(Account account)
    {
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }

    public async Task UpdateAccountAsync(Account account)
    {
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync();
    }

    public async Task<PaymentTransaction?> GetPaymentTransactionAsync(Guid orderId, Guid userId)
    {
        return await _context.PaymentTransactions
            .FirstOrDefaultAsync(t => t.OrderId == orderId && t.UserId == userId);
    }

    public async Task<PaymentTransaction> CreatePaymentTransactionAsync(PaymentTransaction transaction)
    {
        _context.PaymentTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<InboxMessage?> GetInboxMessageByMessageIdAsync(string messageId)
    {
        return await _context.InboxMessages
            .FirstOrDefaultAsync(m => m.MessageId == messageId);
    }

    public async Task<InboxMessage> CreateInboxMessageAsync(InboxMessage message)
    {
        _context.InboxMessages.Add(message);
        await _context.SaveChangesAsync();
        return message;
    }

    public async Task UpdateInboxMessageAsync(InboxMessage message)
    {
        _context.InboxMessages.Update(message);
        await _context.SaveChangesAsync();
    }

    public async Task<List<OutboxMessage>> GetUnsentOutboxMessagesAsync(int limit = 100)
    {
        return await _context.OutboxMessages
            .Where(m => !m.Sent)
            .OrderBy(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<OutboxMessage> CreateOutboxMessageAsync(OutboxMessage message)
    {
        _context.OutboxMessages.Add(message);
        await _context.SaveChangesAsync();
        return message;
    }

    public async Task UpdateOutboxMessageAsync(OutboxMessage message)
    {
        _context.OutboxMessages.Update(message);
        await _context.SaveChangesAsync();
    }
}

