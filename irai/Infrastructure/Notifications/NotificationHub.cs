using Microsoft.AspNetCore.SignalR;

namespace osuRequestor.Infrastructure.Notifications;

public class NotificationHub : Hub<INotificationHub>
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        this._logger = logger;
    }

    public async Task JoinUserGroup(int userId)
    {
        this._logger.LogInformation($"Adding {userId}");
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, $"user_{userId}");
    }

    public async Task JoinServiceGroup(string name)
    {
        this._logger.LogInformation($"Adding {name} to connected services");
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, "service");
    }
}
