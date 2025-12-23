using Microsoft.EntityFrameworkCore;
using PaymentsService.Data;
using PaymentsService.Interfaces;
using PaymentsService.Repositories;
using PaymentsService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=postgres;Database=payments;Username=postgres;Password=postgres";

builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseNpgsql(connectionString));

var rabbitMQConnection = builder.Configuration.GetConnectionString("RabbitMQ") 
    ?? "amqp://guest:guest@rabbitmq:5672/";

builder.Services.AddSingleton<IMessageBus>(sp => new RabbitMQService(rabbitMQConnection));
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    dbContext.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

var messageBus = app.Services.GetRequiredService<IMessageBus>();
var paymentService = app.Services.CreateScope().ServiceProvider.GetRequiredService<IPaymentService>();

messageBus.ConsumeOrderPayment(async (messageId, messageType, body, deliveryTag) =>
{
    try
    {
        using var scope = app.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IPaymentService>();
        await service.ProcessOrderPaymentAsync(messageId, messageType, body);
        messageBus.AckMessage(deliveryTag);
    }
    catch
    {
    }
});

var outboxTimer = new System.Timers.Timer(5000);
outboxTimer.Elapsed += async (sender, e) =>
{
    try
    {
        using var scope = app.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IPaymentService>();
        await service.ProcessOutboxAsync();
    }
    catch
    {
    }
};
outboxTimer.Start();

app.Run();

