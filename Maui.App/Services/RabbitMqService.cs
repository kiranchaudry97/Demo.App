using RabbitMQ.Client;

using System;

using System.Text;

namespace Maui.App.Services;

public sealed class RabbitMqService : IDisposable

{

    private IConnection? _connection;

    private IModel? _channel;
    
    private readonly object _lock = new();

    public RabbitMqService()
    {
    }

    private void Initialize()
    {
        if (_channel != null) return;

        lock (_lock)
        {
            if (_channel != null) return;

            try
            {
                var hostName = "10.2.160.223";

                if (Microsoft.Maui.Devices.DeviceInfo.Platform == Microsoft.Maui.Devices.DevicePlatform.Android)
                {
                     hostName = "10.0.2.2";
                }

                var factory = new ConnectionFactory
                {
                    HostName = hostName, // VM of emulator IP
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RabbitMqService init failed: {ex}");
                _channel = null;
                _connection = null;
            }
        }
    }

    public void Send(string message)
    {
        try
        {
            Initialize();

            if (_channel == null)
            {
                System.Diagnostics.Debug.WriteLine("RabbitMqService: channel is null, message not sent.");
                return;
            }

            var body = Encoding.UTF8.GetBytes(message);
            _channel.BasicPublish(
                exchange: "",
                routingKey: "maui.queue",
                basicProperties: null,
                body: body);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RabbitMqService send failed: {ex}");
        }
    }

    public void Dispose()

    {

        _channel?.Close();

        _channel?.Dispose();

        _connection?.Close();

        _connection?.Dispose();

    }

}

