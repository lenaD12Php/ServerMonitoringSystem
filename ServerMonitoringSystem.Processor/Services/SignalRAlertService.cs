using Microsoft.AspNetCore.SignalR.Client;
using ServerMonitoringSystem.Processor.Interfaces;
using ServerMonitoringSystem.Processor.Models;

namespace ServerMonitoringSystem.Processor.Services;

public class SignalRAlertService : IAlert
{
    private readonly HubConnection _connection;
    private readonly ILogger<SignalRAlertService> _logger;
    private readonly MongoDbService _mongoDbService;

    public SignalRAlertService(IConfiguration config, ILogger<SignalRAlertService> logger, MongoDbService mongoDbService)
    {
        _logger = logger;
        _mongoDbService = mongoDbService;

        var signalRUrl = config["SignalR:Url"] ?? "http://localhost:5000/alertHub";
        
        _connection = new HubConnectionBuilder()
            .WithUrl(signalRUrl)
            .Build();

        _logger.LogInformation("SignalR alert service initialized. Hub URL: {SignalRUrl}", signalRUrl);
    }

    public async Task SendAlertAsync(Alert alert)
    {
        try
        {
            await _mongoDbService.SaveAlertAsync(alert);

            if (_connection.State == HubConnectionState.Disconnected)
                await _connection.StartAsync();
            
            await _connection.InvokeAsync("SendAlert", alert);
            _logger.LogInformation("Alert sent via SignalR: {AlertType} for {ServerIdentifier}", alert.AlertType, alert.ServerIdentifier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending alert for {ServerIdentifier}", alert.ServerIdentifier);
        }
    }

    public async Task<bool> CheckForAnomaliesAsync(ServerStatistics currentStats, ServerStatistics? previousStats)
    {
        if (previousStats == null)
        {
            _logger.LogDebug("No previous statistics available for {ServerIdentifier}, skipping anomaly detection", currentStats.ServerIdentifier);
            return false;
        }

        bool hasAnomaly = false;

        if (currentStats.MemoryUsage > previousStats.MemoryUsage * 1.4) //(1+40%)
        {
            var alert = new Alert
            {
                ServerIdentifier = currentStats.ServerIdentifier,
                AlertType = "MemoryAnomaly",
                Message = $"Memory usage increased by more than 40%. Previous: {previousStats.MemoryUsage:F1}MB, Current: {currentStats.MemoryUsage:F1}MB",
                CurrentValue = currentStats.MemoryUsage,
                PreviousValue = previousStats.MemoryUsage,
                Timestamp = DateTime.UtcNow
            };
            await SendAlertAsync(alert);
            hasAnomaly = true;
        }

        if (currentStats.CpuUsage > previousStats.CpuUsage * 1.5) //(1+50%)
        {
            var alert = new Alert
            {
                ServerIdentifier = currentStats.ServerIdentifier,
                AlertType = "CpuAnomaly",
                Message = $"CPU usage increased by more than 50%. Previous: {previousStats.CpuUsage:F1}%, Current: {currentStats.CpuUsage:F1}%",
                CurrentValue = currentStats.CpuUsage,
                PreviousValue = previousStats.CpuUsage,
                Timestamp = DateTime.UtcNow
            };
            await SendAlertAsync(alert);
            hasAnomaly = true;
        }

        var totalMemory = currentStats.MemoryUsage + currentStats.AvailableMemory;
        var memoryUsagePercentage = (currentStats.MemoryUsage / totalMemory) * 100;
        if (memoryUsagePercentage > 80)
        {
            var alert = new Alert
            {
                ServerIdentifier = currentStats.ServerIdentifier,
                AlertType = "HighMemoryUsage",
                Message = $"High memory usage detected: {memoryUsagePercentage:F1}% ({currentStats.MemoryUsage:F1}MB / {totalMemory:F1}MB)",
                CurrentValue = memoryUsagePercentage,
                PreviousValue = 0,
                Timestamp = DateTime.UtcNow
            };
            await SendAlertAsync(alert);
            hasAnomaly = true;
        }

        if (currentStats.CpuUsage > 90)
        {
            var alert = new Alert
            {
                ServerIdentifier = currentStats.ServerIdentifier,
                AlertType = "HighCpuUsage",
                Message = $"High CPU usage detected: {currentStats.CpuUsage:F1}%",
                CurrentValue = currentStats.CpuUsage,
                PreviousValue = 0,
                Timestamp = DateTime.UtcNow
            };
            await SendAlertAsync(alert);
            hasAnomaly = true;
        }

        return hasAnomaly;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }
    }
}
