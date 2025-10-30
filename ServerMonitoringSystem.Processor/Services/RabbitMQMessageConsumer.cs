using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ServerMonitoringSystem.Processor.Interfaces;
using ServerMonitoringSystem.Processor.Models;
using System.Text;
using System.Text.Json;

namespace ServerMonitoringSystem.Processor.Services;

public class RabbitMQMessageConsumer : IMessageConsumer
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQMessageConsumer> _logger;
    private readonly IAlert _alertService;
    private readonly string _queueName;
    private readonly string _exchangeName;
    private readonly Dictionary<string, ServerStatistics> _lastStatistics = new();

    public event EventHandler<ServerStatistics>? StatisticsReceived;

    public RabbitMQMessageConsumer(
        IConfiguration config,
        ILogger<RabbitMQMessageConsumer> logger,
        IAlert alertService)
    {
        _logger = logger;
        _alertService = alertService;

        var hostName = config["Messaging:HostName"];
        var userName = config["Messaging:UserName"];
        var password = config["Messaging:Password"];
        _exchangeName = config["Messaging:ExchangeName"];
        _queueName = config["Messaging:QueueName"];

        var factory = new ConnectionFactory
        {
            HostName = hostName,
            UserName = userName,
            Password = password
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(exchange: _exchangeName, type: ExchangeType.Topic, durable: true);
        _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue: _queueName, exchange: _exchangeName, routingKey: "ServerStatistics.*");

        _logger.LogInformation("RabbitMQ consumer initialized. Queue: {QueueName} is listening", _queueName);
    }

    public Task StartConsumingAsync(CancellationToken cancellationToken = default)
    {
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, basicDeliverEventArgs) =>
        {
            try
            {
                var body = basicDeliverEventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var routingKey = basicDeliverEventArgs.RoutingKey;

                _logger.LogDebug("Received message: {Message} with routing key: {RoutingKey}", message, routingKey);

                var serverIdentifier = routingKey.Replace("ServerStatistics.", "");

                var statistics = JsonSerializer.Deserialize<ServerStatistics>(message);
                if (statistics != null)
                {
                    statistics.ServerIdentifier = serverIdentifier;
                    statistics.Timestamp = DateTime.UtcNow;

                    var previousStatistics = _lastStatistics.ContainsKey(serverIdentifier) ? _lastStatistics[serverIdentifier] : null;

                    var hasAnomaly = await _alertService.CheckForAnomaliesAsync(statistics, previousStatistics);
                    if (hasAnomaly)
                    {
                        _logger.LogWarning("Anomaly detected for server {ServerIdentifier}", serverIdentifier);
                    }

                    _lastStatistics[serverIdentifier] = statistics;

                    StatisticsReceived?.Invoke(this, statistics);

                    _logger.LogInformation("Processed statistics for server {ServerIdentifier}: CPU={CpuUsage:F1}%, Memory={MemoryUsage:F1}MB", 
                        serverIdentifier, statistics.CpuUsage, statistics.MemoryUsage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {Message}", basicDeliverEventArgs.Body.ToArray());
            }
        };

        _channel.BasicConsume(queue: _queueName, autoAck: true, consumer: consumer);
        _logger.LogInformation("Started consuming messages from RabbitMQ");
        return Task.CompletedTask;
    }

    public async Task StopConsumingAsync()
    {
        _logger.LogInformation("Stopping RabbitMQ consumer");
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        try
        {
            _channel?.Close();
            _connection?.Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ consumer");
        }
        finally
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
