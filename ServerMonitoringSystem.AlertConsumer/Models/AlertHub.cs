using Microsoft.AspNetCore.SignalR;

namespace ServerMonitoringSystem.AlertConsumer.Models;

public class AlertHub : Hub
{
    public async Task SendAlert(Alert alert)
    {
        await Clients.All.SendAsync("Send Alert", alert);
    }
}
