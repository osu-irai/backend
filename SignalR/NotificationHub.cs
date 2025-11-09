using Microsoft.AspNetCore.SignalR;

namespace osuRequestor.SignalR;

public class NotificationHub : Hub<INotificationHub>
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinUserGroup(int userId)
    {
        _logger.LogInformation($"Adding {userId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
    }

    public async Task JoinServiceGroup(string name)
    {
        _logger.LogInformation($"Adding {name} to connected services");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"service");
    }
    
}