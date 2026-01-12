using RabbitMQ.Client;

using System;

using System.Text;

namespace Maui.App.Services;

public sealed class RabbitMqService : IDisposable

{

    private IConnection? _connection;

    private IModel? _channel;

    public RabbitMqService()

    {

        var factory = new ConnectionFactory

        {

            HostName = "10.2.160.223", // VM of emulator IP

            Port = 5672,

            UserName = "guest",

            Password = "guest",

            AutomaticRecoveryEnabled = true

        };

        _connection = factory.CreateConnection();

        _channel = _connection.CreateModel();

        _channel.QueueDeclare(

            queue: "maui.queue",

            durable: false,

            exclusive: false,

            autoDelete: false,

            arguments: null);

    }

    public void Send(string message)

    {

        if (_channel == null) throw new InvalidOperationException("Channel is null");

        var body = Encoding.UTF8.GetBytes(message);

        _channel.BasicPublish(

            exchange: "",

            routingKey: "maui.queue",

            basicProperties: null,

            body: body);

    }

    public void Dispose()

    {

        _channel?.Close();

        _channel?.Dispose();

        _connection?.Close();

        _connection?.Dispose();

    }

}

