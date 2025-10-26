namespace ServerMonitoringSystem.Processor.Interfaces;

public interface IMessageConsumer : IDisposable
{
    Task StartConsumingAsync(CancellationToken cancellationToken = default);
    Task StopConsumingAsync();
}
