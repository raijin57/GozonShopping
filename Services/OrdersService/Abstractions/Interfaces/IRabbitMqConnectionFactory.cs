using RabbitMQ.Client;

namespace OrdersService.Abstractions.Interfaces;

public interface IRabbitMqConnectionFactory
{
    IConnection Create();
}

