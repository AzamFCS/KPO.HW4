using Microsoft.EntityFrameworkCore;
using PaymentsService.Data;
using PaymentsService.DTOs;
using PaymentsService.Interfaces;
using PaymentsService.Messages;
using PaymentsService.Models;
using System.Text;
using System.Text.Json;

namespace PaymentsService.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repository;
    private readonly IMessageBus _messageBus;

    public PaymentService(IPaymentRepository repository, IMessageBus messageBus)
    {
        _repository = repository;
        _messageBus = messageBus;
    }

    public async Task<Account?> CreateAccountAsync(Guid userId)
    {
        var existingAccount = await _repository.GetAccountByUserIdAsync(userId);
        if (existingAccount != null)
        {
            return null;
        }

        var account = new Account
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Balance = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.CreateAccountAsync(account);
        return account;
    }

    public async Task<bool> TopUpAccountAsync(Guid userId, decimal amount)
    {
        var account = await _repository.GetAccountByUserIdAsync(userId);
        if (account == null)
        {
            return false;
        }

        account.Balance += amount;
        await _repository.UpdateAccountAsync(account);
        return true;
    }

    public async Task<BalanceResponse?> GetBalanceAsync(Guid userId)
    {
        var account = await _repository.GetAccountByUserIdAsync(userId);
        if (account == null)
        {
            return null;
        }

        return new BalanceResponse
        {
            UserId = userId,
            Balance = account.Balance
        };
    }

    public async Task<bool> ProcessOrderPaymentAsync(string messageId, string messageType, byte[] body)
    {
        using var transaction = await _repository.BeginTransactionAsync();
        try
        {
            var existingInbox = await _repository.GetInboxMessageByMessageIdAsync(messageId);

            if (existingInbox != null && existingInbox.Processed)
            {
                await transaction.CommitAsync();
                return true;
            }

            if (existingInbox == null)
            {
                var inboxMessage = new InboxMessage
                {
                    Id = Guid.NewGuid(),
                    MessageId = messageId,
                    MessageType = messageType,
                    Payload = Encoding.UTF8.GetString(body),
                    Processed = false,
                    ReceivedAt = DateTime.UtcNow
                };
                await _repository.CreateInboxMessageAsync(inboxMessage);
                existingInbox = inboxMessage;
            }

            var request = JsonSerializer.Deserialize<OrderPaymentRequest>(
                existingInbox.Payload);

            if (request == null)
            {
                await transaction.RollbackAsync();
                return false;
            }

            var existingTransaction = await _repository.GetPaymentTransactionAsync(
                request.OrderId, request.UserId);

            if (existingTransaction != null)
            {
                existingInbox.Processed = true;
                existingInbox.ProcessedAt = DateTime.UtcNow;
                await _repository.UpdateInboxMessageAsync(existingInbox);
                await transaction.CommitAsync();
                return true;
            }

            var account = await _repository.GetAccountByUserIdAsync(request.UserId);
            string status;

            if (account == null)
            {
                status = "fail";
            }
            else if (account.Balance < request.Amount)
            {
                status = "fail";
            }
            else
            {
                account.Balance -= request.Amount;
                await _repository.UpdateAccountAsync(account);
                
                var paymentTransaction = new PaymentTransaction
                {
                    Id = Guid.NewGuid(),
                    OrderId = request.OrderId,
                    UserId = request.UserId,
                    Amount = request.Amount,
                    CreatedAt = DateTime.UtcNow
                };
                await _repository.CreatePaymentTransactionAsync(paymentTransaction);
                status = "success";
            }

            var paymentStatusEvent = new PaymentStatusEvent
            {
                OrderId = request.OrderId,
                Status = status
            };

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                MessageType = "PaymentStatusEvent",
                Payload = JsonSerializer.Serialize(paymentStatusEvent),
                Sent = false,
                CreatedAt = DateTime.UtcNow
            };
            await _repository.CreateOutboxMessageAsync(outboxMessage);

            existingInbox.Processed = true;
            existingInbox.ProcessedAt = DateTime.UtcNow;
            await _repository.UpdateInboxMessageAsync(existingInbox);

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task ProcessOutboxAsync()
    {
        var unsentMessages = await _repository.GetUnsentOutboxMessagesAsync(100);

        foreach (var message in unsentMessages)
        {
            try
            {
                _messageBus.PublishPaymentStatus(message.MessageType, message.Payload);
                message.Sent = true;
                message.SentAt = DateTime.UtcNow;
                await _repository.UpdateOutboxMessageAsync(message);
            }
            catch
            {
            }
        }
    }
}

