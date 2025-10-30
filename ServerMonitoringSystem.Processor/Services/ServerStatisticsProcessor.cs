using ServerMonitoringSystem.Processor.Interfaces;

namespace ServerMonitoringSystem.Processor.Services;

public class ServerStatisticsProcessor : BackgroundService
{
    private readonly ILogger<ServerStatisticsProcessor> _logger;
    private readonly IMessageConsumer _messageConsumer;
    private readonly MongoDbService _mongoDbService;

    public ServerStatisticsProcessor(
        ILogger<ServerStatisticsProcessor> logger,
        IMessageConsumer messageConsumer,
        MongoDbService mongoDbService)
    {
        _logger = logger;
        _messageConsumer = messageConsumer;
        _mongoDbService = mongoDbService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Message Processing and Anomaly Detection Service");

        if (_messageConsumer is RabbitMQMessageConsumer rabbitConsumer)
        {
            rabbitConsumer.StatisticsReceived += async (sender, statistics) =>
            {
                try
                {
                    await _mongoDbService.SaveServerStatisticsAsync(statistics);
                    _logger.LogDebug("Saved statistics for server {ServerIdentifier}", statistics.ServerIdentifier);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing statistics for server {ServerIdentifier}", statistics.ServerIdentifier);
                }
            };
        }

        await _messageConsumer.StartConsumingAsync(stoppingToken);

        _logger.LogInformation("Message Processing Service started successfully");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Message Processing Service");
        await _messageConsumer.StopConsumingAsync();
        await base.StopAsync(cancellationToken);
    }
}