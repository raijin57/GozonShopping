using PaymentsService.Abstractions.Interfaces;
using RabbitMQ.Client;

namespace PaymentsService.Infrastructure.Messaging;

public sealed class RabbitMqMessagePublisher(IRabbitMqConnectionFactory connectionFactory) : IMessagePublisher, IDisposable
{
    private readonly Lazy<IConnection> _connection = new(connectionFactory.Create);

    public IModel CreateChannel() => _connection.Value.CreateModel();

    public Task PublishAsync(string exchange, string routingKey, string messageType, ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        using var channel = CreateChannel();
        channel.ExchangeDeclare(exchange, ExchangeType.Fanout, durable: true);

        var props = channel.CreateBasicProperties();
        props.MessageId = Guid.NewGuid().ToString();
        props.Type = messageType;
        props.DeliveryMode = 2;

        channel.BasicPublish(exchange, routingKey ?? string.Empty, props, body);
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

