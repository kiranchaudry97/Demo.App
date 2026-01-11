using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
#if RABBITMQ
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
#endif

namespace Demo.App.Services;

public class RabbitMqConsumer : BackgroundService
{
    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly IConfiguration _config;
    private dynamic? _connection;
    private dynamic? _channel;
    private string _queue = "orders.queue";

    public RabbitMqConsumer(IConfiguration config, ILogger<RabbitMqConsumer> logger)
    {
        _config = config;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
#if RABBITMQ
        var host = _config["RabbitMq:Host"] ?? "localhost";
        var port = int.TryParse(_config["RabbitMq:Port"], out var p) ? p : 5672;
        var user = _config["RabbitMq:Username"] ?? "guest";
        var pass = _config["RabbitMq:Password"] ?? "guest";
        var vhost = _config["RabbitMq:VirtualHost"] ?? "/";
        var exchange = _config["RabbitMq:Exchange"] ?? "orders.exchange";
        var routing = _config["RabbitMq:RoutingKey"] ?? "orders.created";
        _queue = _config["RabbitMq:Queue"] ?? _queue;

        var factory = new ConnectionFactory { HostName = host, Port = port, UserName = user, Password = pass, VirtualHost = vhost };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true, autoDelete: false);
        _channel.QueueDeclare(_queue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(_queue, exchange, routing);

        _logger.LogInformation("RabbitMQ consumer connected to {Host}:{Port}, queue={Queue}", host, port, _queue);

        return base.StartAsync(cancellationToken);
#else
        _logger.LogInformation("RabbitMQ consumer disabled because RABBITMQ symbol not defined.");
        return base.StartAsync(cancellationToken);
#endif
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
#if RABBITMQ
        if (_channel == null) return Task.CompletedTask;

        // simple polling loop (keeps implementation minimal)
        return Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _channel.BasicGet(_queue, autoAck: false);
                    if (result != null)
                    {
                        var body = result.Body.ToArray();
                        var json = Encoding.UTF8.GetString(body);
                        _logger.LogInformation("RabbitMQ message received: {Message}", json);
                        _channel.BasicAck(result.DeliveryTag, multiple: false);
                    }
                    else
                    {
                        await Task.Delay(500, stoppingToken);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while polling RabbitMQ");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }, stoppingToken);
#else
        // no-op when RABBITMQ symbol not set
        return Task.CompletedTask;
#endif
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _channel?.Close();
            _connection?.Close();
        }
        catch { }
        return base.StopAsync(cancellationToken);
    }
}
