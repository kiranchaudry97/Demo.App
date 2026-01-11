using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using Demo.Shared.Events;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHttpClient();
        services.AddSingleton<SalesforceService>();
        services.AddHostedService<Worker>();
    })
    .ConfigureLogging(logging => logging.AddConsole())
    .Build();

await host.RunAsync();

public class Worker : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ILogger<Worker> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly SalesforceService _salesforce;

    public Worker(IConfiguration config, ILogger<Worker> logger, SalesforceService salesforce)
    {
        _config = config;
        _logger = logger;
        _salesforce = salesforce;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var host = _config["RabbitMq:Host"] ?? "localhost";
        var port = int.TryParse(_config["RabbitMq:Port"], out var p) ? p : 5672;
        var user = _config["RabbitMq:Username"] ?? "guest";
        var pass = _config["RabbitMq:Password"] ?? "guest";
        var vhost = _config["RabbitMq:VirtualHost"] ?? "/";
        var exchange = _config["RabbitMq:Exchange"] ?? "orders.exchange";
        var routing = _config["RabbitMq:RoutingKey"] ?? "orders.created";
        var queue = _config["RabbitMq:Queue"] ?? "orders.queue";

        var factory = new ConnectionFactory { HostName = host, Port = port, UserName = user, Password = pass, VirtualHost = vhost };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true, autoDelete: false);
        _channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue, exchange, routing);

        _logger.LogInformation("Worker connected to RabbitMQ {Host}:{Port}, queue={Queue}", host, port, queue);

        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel == null) return Task.CompletedTask;

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (sender, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var evt = JsonConvert.DeserializeObject<OrderCreatedEvent>(json);
                _logger.LogInformation("Received order event {EventId} for order {OrderId}", evt?.EventId, evt?.OrderId);

                if (evt != null)
                {
                    var (ok, externalId) = await _salesforce.CreateOrderAsync(evt);
                    if (ok)
                    {
                        _logger.LogInformation("Order {OrderId} processed for Salesforce, externalId={ExternalId}", evt.OrderId, externalId);
                    }
                    else
                    {
                        _logger.LogWarning("Order {OrderId} failed to process in Salesforce, will requeue", evt.OrderId);
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                        return;
                    }
                }

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(queue: _config["RabbitMq:Queue"] ?? "orders.queue", autoAck: false, consumer: consumer);
        return Task.CompletedTask;
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
