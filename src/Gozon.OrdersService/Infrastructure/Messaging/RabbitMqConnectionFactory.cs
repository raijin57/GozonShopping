using Microsoft.Extensions.Options;
using OrdersService.Abstractions.Interfaces;
using OrdersService.Infrastructure.Options;
using RabbitMQ.Client;

namespace OrdersService.Infrastructure.Messaging;

public sealed class RabbitMqConnectionFactory(IOptions<RabbitMqOptions> options) : IRabbitMqConnectionFactory
{
    private readonly RabbitMqOptions _settings = options.Value;

    public IConnection Create()
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            DispatchConsumersAsync = true
        };

        return factory.CreateConnection();
    }
}

