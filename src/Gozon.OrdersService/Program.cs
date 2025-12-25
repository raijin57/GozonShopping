using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OrdersService.Abstractions.Interfaces;
using OrdersService.Features.Notifications;
using OrdersService.Features.Orders;
using OrdersService.Features.Payments;
using OrdersService.Infrastructure.Background;
using OrdersService.Infrastructure.Data;
using OrdersService.Infrastructure.Messaging;
using OrdersService.Infrastructure.Options;
using OrdersService.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddDbContext<OrdersDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("OrdersDb") ??
                           "Host=localhost;Port=5433;Database=orders;Username=postgres;Password=postgres";
    options.UseNpgsql(connectionString);
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

builder.Services.AddScoped<IOrdersRepository, OrdersRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentStatusProcessor, PaymentStatusProcessor>();
builder.Services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
builder.Services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();

builder.Services.AddHostedService<OutboxDispatcher>();
builder.Services.AddHostedService<PaymentStatusListener>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials().SetIsOriginAllowed(_ => true);
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();
app.MapOpenApi();

app.MapControllers();
app.MapHub<OrderStatusHub>("/hubs/orders");

app.Run();
