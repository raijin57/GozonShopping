using OrdersService.Abstractions.Interfaces;
using RabbitMQ.Client;

namespace OrdersService.Infrastructure.Messaging;

public sealed class RabbitMqMessagePublisher(IRabbitMqConnectionFactory connectionFactory) : IMessagePublisher, IDisposable
{
    private readonly Lazy<IConnection> _connection = new(connectionFactory.Create);

    public IModel CreateChannel() => _connection.Value.CreateModel();

    public Task PublishAsync(string exchange, string routingKey, string messageType, ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        using var channel = CreateChannel();
        channel.ExchangeDeclare(exchange, ExchangeType.Fanout, durable: true);

        var properties = channel.CreateBasicProperties();
        properties.MessageId = Guid.NewGuid().ToString();
        properties.Type = messageType;
        properties.DeliveryMode = 2; // persistent

        channel.BasicPublish(exchange: exchange, routingKey: routingKey ?? string.Empty, basicProperties: properties, body: body);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_connection.IsValueCreated)
        {
            _connection.Value.Dispose();
        }
    }
}

