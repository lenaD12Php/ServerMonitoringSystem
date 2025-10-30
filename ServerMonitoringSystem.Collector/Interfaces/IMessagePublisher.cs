namespace ServerMonitoringSystem.Collector.Interfaces;

public interface IMessagePublisher : IAsyncDisposable
{
    Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken);
}
