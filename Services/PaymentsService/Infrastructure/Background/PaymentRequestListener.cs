using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaymentsService.Abstractions.Interfaces;
using PaymentsService.Infrastructure.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Messages;

namespace PaymentsService.Infrastructure.Background;

public sealed class PaymentRequestListener(
    IRabbitMqConnectionFactory connectionFactory,
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<PaymentRequestListener> logger) : BackgroundService
{
    private readonly RabbitMqOptions _settings = options.Value;
    private IConnection? _connection;
    private IModel? _channel;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _connection = connectionFactory.Create();
        _channel = _connection.CreateModel();

        // Подписка на задачи оплаты заказов из Orders
        _channel.ExchangeDeclare(_settings.OrdersExchange, ExchangeType.Fanout, durable: true);
        var queue = _channel.QueueDeclare(queue: "payments.orders.request", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue.QueueName, _settings.OrdersExchange, routingKey: string.Empty);
        _channel.BasicQos(0, 10, false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, args) =>
        {
            var json = Encoding.UTF8.GetString(args.Body.ToArray());
            try
            {
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IPaymentProcessor>();
                var message = JsonSerializer.Deserialize<OrderPaymentRequested>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                if (message is null)
                {
                    logger.LogWarning("Received empty payment request");
                    _channel.BasicAck(args.DeliveryTag, multiple: false);
                    return;
                }

                await processor.ProcessAsync(message, stoppingToken);
                _channel.BasicAck(args.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process payment request message");
                _channel.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(queue: queue.QueueName, autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}

