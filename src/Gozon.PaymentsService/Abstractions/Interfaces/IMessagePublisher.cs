using RabbitMQ.Client;

namespace PaymentsService.Abstractions.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync(string exchange, string routingKey, string messageType, ReadOnlyMemory<byte> body, CancellationToken cancellationToken);

    IModel CreateChannel();
}

