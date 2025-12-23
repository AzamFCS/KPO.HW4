using Microsoft.EntityFrameworkCore;
using OrdersService.Data;
using OrdersService.Hubs;
using OrdersService.Interfaces;
using OrdersService.Repositories;
using OrdersService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var redisConnection = builder.Configuration.GetConnectionString("Redis")
    ?? "redis:6379";
builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConnection, options =>
    {
        options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("GozonOrders");
    });
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:5003", "http://localhost:5000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=postgres;Database=orders;Username=postgres;Password=postgres";

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(connectionString));

var rabbitMQConnection = builder.Configuration.GetConnectionString("RabbitMQ")
    ?? "amqp://guest:guest@rabbitmq:5672/";

builder.Services.AddSingleton<IMessageBus>(sp => new RabbitMQService(rabbitMQConnection));
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    dbContext.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();
app.MapHub<OrderStatusHub>("/orderStatusHub");

var messageBus = app.Services.GetRequiredService<IMessageBus>();
var orderService = app.Services.CreateScope().ServiceProvider.GetRequiredService<IOrderService>();
var notificationService = app.Services.CreateScope().ServiceProvider.GetRequiredService<INotificationService>();

messageBus.ConsumePaymentStatus(async (messageType, messageType2, body) =>
{
    try
    {
        using var scope = app.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var notifService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        await service.ProcessPaymentStatusAsync(messageType, messageType2, body);

        var paymentStatus = System.Text.Json.JsonSerializer.Deserialize<OrdersService.Messages.PaymentStatusEvent>(
            System.Text.Encoding.UTF8.GetString(body));
        if (paymentStatus != null)
        {
            var status = paymentStatus.Status == "success" ? "FINISHED" : "CANCELLED";
            await notifService.NotifyOrderStatusChangeAsync(paymentStatus.OrderId, status);
        }
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
        var service = scope.ServiceProvider.GetRequiredService<IOrderService>();
        await service.ProcessOutboxAsync();
    }
    catch
    {
    }
};
outboxTimer.Start();

app.Run();

