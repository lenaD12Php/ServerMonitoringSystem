namespace ServerMonitoringSystem.AlertConsumer.Models;

public class Alert
{
    public string? Id { get; set; }
    public string ServerId { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty; // MemoryAnomaly,CpuAnomaly,HighMemoryUsage,HighCpuUsage
    public string Message { get; set; } = string.Empty;
    public double CurrentValue { get; set; }
    public double PreviousValue { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsResolved { get; set; } = false;
}
