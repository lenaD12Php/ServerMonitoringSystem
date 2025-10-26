using System.Diagnostics;
using System.Timers;
using ServerMonitoringSystem.Collector.Interfaces;
using ServerMonitoringSystem.Collector.Models;

public sealed class ServerStatisticsCollector : IDisposable
{
    private readonly System.Timers.Timer _timer;
    private readonly PerformanceCounter _cpu;
    private readonly PerformanceCounter _memAvailable;
    private readonly PerformanceCounter _memTotal;
    private readonly int _samplingIntervalSeconds;
    private readonly double _totalPhysicalMemInMb;

    private readonly IMessagePublisher _publisher;
    private readonly string _topic;

    public event EventHandler<ServerStatistics>? StatisticsCollected;

    public ServerStatisticsCollector(IConfiguration config, IMessagePublisher publisher)
    {
        _publisher = publisher;

        _samplingIntervalSeconds = config.GetValue<int>("ServerStatisticsConfig:SamplingIntervalSeconds");

        var serverId = config.GetValue<string>("ServerStatisticsConfig:ServerIdentifier");
        _topic = $"ServerStatistics.{serverId}";

        _timer = new System.Timers.Timer(_samplingIntervalSeconds * 1000) { AutoReset = true };
        _timer.Elapsed += OnTimed;

        _cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _memAvailable = new PerformanceCounter("Memory", "Available MBytes");
        _memTotal = new PerformanceCounter("Memory", "Committed Bytes");
        _ = _cpu.NextValue();

        // Get total physical memory using PerformanceCounter
        _totalPhysicalMemInMb = _memTotal.NextValue() / (1024d * 1024d);
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();

    private void OnTimed(object? sender, ElapsedEventArgs e)
    {
            var cpu = _cpu.NextValue();
            System.Threading.Thread.Sleep(1000);      // need 2 reads for accurate collection
            cpu = _cpu.NextValue();

            var availableMb = _memAvailable.NextValue();
            var usedMb = Math.Max(0, _totalPhysicalMemInMb - availableMb);

            var statistics = new ServerStatistics
            {
                MemoryUsage = usedMb,
                AvailableMemory = availableMb,
                CpuUsage = cpu,
                Timestamp = DateTime.UtcNow
            };

            StatisticsCollected?.Invoke(this, statistics);

            _ = PublishAsync(statistics);
    }

    private async Task PublishAsync(ServerStatistics statistics)
    {
         await _publisher.PublishAsync(_topic, statistics, CancellationToken.None);
    }

    public void Dispose()
    {
        _timer.Elapsed -= OnTimed;
        _timer.Dispose();
        _cpu.Dispose();
        _memAvailable.Dispose();
        _memTotal.Dispose();
    }
}
