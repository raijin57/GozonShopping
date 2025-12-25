using RabbitMQ.Client;

namespace PaymentsService.Abstractions.Interfaces;

public interface IRabbitMqConnectionFactory
{
    IConnection Create();
}

