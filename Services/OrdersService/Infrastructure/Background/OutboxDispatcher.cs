using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrdersService.Abstractions.Interfaces;
using OrdersService.Infrastructure.Options;

namespace OrdersService.Infrastructure.Background;

public sealed class OutboxDispatcher(
    IOutboxRepository outboxRepository,
    IMessagePublisher publisher,
    IOptions<RabbitMqOptions> options,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    private readonly RabbitMqOptions _settings = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Достаём из outbox все сообщения, что ещё не отправлены в брокер
            var messages = await outboxRepository.GetUnprocessedAsync(50, stoppingToken);
            if (messages.Count == 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                continue;
            }

            foreach (var message in messages)
            {
                try
                {
                    // Отправка в RabbitMQ и пометка как доставленное
                    var body = Encoding.UTF8.GetBytes(message.Payload);
                    await publisher.PublishAsync(_settings.OrdersExchange, string.Empty, message.Type, body, stoppingToken);
                    await outboxRepository.MarkAsProcessedAsync(message, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to dispatch outbox message {MessageId}", message.Id);
                    message.Attempt += 1;
                    message.Error = ex.Message;
                }
            }

            await outboxRepository.SaveChangesAsync(stoppingToken);
        }
    }
}

