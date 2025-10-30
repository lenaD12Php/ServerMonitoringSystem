using ServerMonitoringSystem.Processor.Interfaces;
using ServerMonitoringSystem.Processor.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddSingleton<IAlert, SignalRAlertService>();
builder.Services.AddSingleton<IMessageConsumer, RabbitMQMessageConsumer>();
builder.Services.AddHostedService<ServerStatisticsProcessor>();

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Message Processor starting");

host.Run();