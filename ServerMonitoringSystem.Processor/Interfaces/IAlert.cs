using ServerMonitoringSystem.Processor.Models;

namespace ServerMonitoringSystem.Processor.Interfaces;

public interface IAlert
{
    Task SendAlertAsync(Alert alert);
    Task<bool> CheckForAnomaliesAsync(ServerStatistics currentStats, ServerStatistics? previousStats);
}
