using ServerMonitoringSystem.Collector.Interfaces;
using ServerMonitoringSystem.Collector.Models;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
builder.Services.AddSingleton<ServerStatisticsCollector>();

using IHost host = builder.Build();

var collector = host.Services.GetRequiredService<ServerStatisticsCollector>();

collector.StatisticsCollected += (sender, statistics) =>
{
    Console.WriteLine(
        $"[{statistics.Timestamp:HH:mm:ss}] CPU Usage: {statistics.CpuUsage:F1}% , " +
        $"Memory Usage: {statistics.MemoryUsage:F1} MB , Available Memory: {statistics.AvailableMemory:F1} MB");
};

Console.WriteLine("Starting Statistics Collector");
collector.Start();

await host.RunAsync();

collector.Stop();
Console.WriteLine("Statistics Collector stopped.");
