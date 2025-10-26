using Microsoft.Extensions.Configuration;               
using RabbitMQ.Client;
using ServerMonitoringSystem.Collector.Interfaces;
using System.Text.Json;

namespace ServerMonitoringSystem.Collector.Models
{
    public sealed class RabbitMqMessagePublisher : IMessagePublisher, IAsyncDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _exchangeName;
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        public RabbitMqMessagePublisher(IConfiguration config)
        {
            if (config is null) throw new ArgumentNullException(nameof(config));

            // Read from appsettings.json -> "Messaging"
            var hostName = config["Messaging:HostName"] ?? "localhost";
            var userName = config["Messaging:UserName"] ?? "guest";
            var password = config["Messaging:Password"] ?? "guest";
            _exchangeName = config["Messaging:ExchangeName"] ?? "amq.topic";
            var durable = bool.TryParse(config["Messaging:Durable"], out var d) && d;

            var factory = new ConnectionFactory
            {
                HostName = hostName,
                UserName = userName,
                Password = password
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            if (!_exchangeName.StartsWith("amq."))
            {
                _channel.ExchangeDeclare(exchange: _exchangeName, type: ExchangeType.Topic, durable: durable);
            }
        }

        public Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
        {
            var body = JsonSerializer.SerializeToUtf8Bytes(message, _jsonOptions);

            var properties = _channel.CreateBasicProperties();
            properties.ContentType = "application/json";
            properties.DeliveryMode = 2; // persistent to make sure the if anything happens the message will not be lost 

            _channel.BasicPublish(
                exchange: _exchangeName,
                routingKey: topic,
                mandatory: false,
                basicProperties: properties,
                body: body);

            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            try { _channel?.Close(); } catch { }
            try { _connection?.Close(); } catch { }
            _channel?.Dispose();
            _connection?.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
