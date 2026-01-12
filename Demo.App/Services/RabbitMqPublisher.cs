using System;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
#if RABBITMQ
using RabbitMQ.Client;
#endif
using Newtonsoft.Json;

namespace Demo.App.Services;

public class RabbitMqPublisher : IDisposable, IRabbitMqPublisher
{
    private readonly dynamic _connection;
    private readonly dynamic _channel;
    private readonly string _exchange;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(IConfiguration config, ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;
        var host = config["RabbitMq:Host"] ?? "localhost";
        var port = int.TryParse(config["RabbitMq:Port"], out var p) ? p : 5672;
        var user = config["RabbitMq:Username"] ?? "guest";
        var pass = config["RabbitMq:Password"] ?? "guest";
        var vhost = config["RabbitMq:VirtualHost"] ?? "/";
        _exchange = config["RabbitMq:Exchange"] ?? "orders.exchange";
#if RABBITMQ
        var factory = new ConnectionFactory()
        {
            HostName = host,
            Port = port,
            UserName = user,
            Password = pass,
            VirtualHost = vhost
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // ensure durable exchange exists
        _channel.ExchangeDeclare(_exchange, ExchangeType.Topic, durable: true, autoDelete: false);
#else
        // RabbitMQ not enabled at compile time; keep fields null for no-op implementation
        _connection = null;
        _channel = null;
#endif
    }

    public void PublishOrderCreated(object evt, string routingKey)
    {
#if RABBITMQ
        var json = JsonConvert.SerializeObject(evt);
        var body = Encoding.UTF8.GetBytes(json);

        var props = _channel.CreateBasicProperties();
        props.DeliveryMode = 2; // persistent
        props.ContentType = "application/json";
        props.MessageId = Guid.NewGuid().ToString();

        _channel.BasicPublish(_exchange, routingKey, basicProperties: props, body: body);
        _logger.LogInformation("Published OrderCreated event for payload, routing {RoutingKey}", routingKey);
#else
        _logger.LogInformation("RabbitMQ disabled - would publish event to {RoutingKey}: {Payload}", routingKey, JsonConvert.SerializeObject(evt));
#endif
    }

    public void Dispose()
    {
        try
        {
            try { _channel?.Dispose(); } catch { }
            try { _connection?.Dispose(); } catch { }
        }
        catch { }
    }
}
