using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Abstractions.Interfaces;
using PaymentsService.Features.Accounts;
using PaymentsService.Features.Payments;
using PaymentsService.Infrastructure.Background;
using PaymentsService.Infrastructure.Data;
using PaymentsService.Infrastructure.Messaging;
using PaymentsService.Infrastructure.Options;
using PaymentsService.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddDbContext<PaymentsDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("PaymentsDb") ??
                           "Host=localhost;Port=5434;Database=payments;Username=postgres;Password=postgres";
    options.UseNpgsql(connectionString);
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

builder.Services.AddScoped<IAccountsRepository, AccountsRepository>();
builder.Services.AddScoped<IInboxRepository, InboxRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IAccountBalanceService, AccountBalanceService>();
builder.Services.AddScoped<IAccountsService, AccountService>();
builder.Services.AddScoped<IPaymentProcessor, PaymentProcessor>();
builder.Services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
builder.Services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();

builder.Services.AddHostedService<OutboxDispatcher>();
builder.Services.AddHostedService<PaymentRequestListener>();

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
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();
app.MapOpenApi();

app.MapControllers();

app.Run();
