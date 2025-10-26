//using Microsoft.AspNetCore.Builder;
//using Microsoft.Extensions.DependencyInjection;
//using ServerMonitoringSystem.AlertConsumer.Models;

//var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddSignalR();
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowAll", policy =>
//        policy.AllowAnyOrigin()
//              .AllowAnyMethod()
//              .AllowAnyHeader());
//});

//var app = builder.Build();

//app.UseCors("AllowAll");
//app.MapHub<AlertHub>("/alertHub");

//app.Run();

using Microsoft.AspNetCore.SignalR;
using ServerMonitoringSystem.AlertConsumer.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseRouting();

app.MapHub<AlertHub>("/alertHub");

app.MapGet("/", () => "Server Monitoring SignalR Hub is running!");

Console.WriteLine("Starting SignalR Hub server");

app.Run("http://localhost:5000");
