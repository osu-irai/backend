using Microsoft.AspNetCore.SignalR;
using osuRequestor.Features.Requests.Mappings;
using osuRequestor.Features.Requests.Models;

namespace osuRequestor.Infrastructure.Notifications;

public class RequestNotificationService : IRequestNotificationService
{
    private readonly IHubContext<NotificationHub, INotificationHub> _hub;
    private readonly ILogger<RequestNotificationService> _logger;

    public RequestNotificationService(IHubContext<NotificationHub, INotificationHub> hub,
                                      ILogger<RequestNotificationService> logger)
    {
        this._hub    = hub;
        this._logger = logger;
    }

    #region IRequestNotificationService Members

    public async Task NotifyUserAsync(string destinationUsername, ReceivedRequestResponse request)
    {
        this._logger.LogInformation($"Sending request to {destinationUsername}");
        await this._hub.Clients.Group($"user_{destinationUsername}")
                  .ReceiveRequest(request);

        await this._hub.Clients.Group("service")
                  .ReceiveFullRequest(request.ToRequest(destinationUsername));
    }

    public async Task NotifyAllAsync(string message)
    {
        this._logger.LogInformation($"Notifying all users with {message}");
        await this._hub.Clients.All.ReceiveGlobalNotification(message);
    }

    public async Task NotifyAboutIrcChange(string username, bool newIrcStatus)
    {
        this._logger.LogInformation($"User {username} toggled IRC");
        await this._hub.Clients.Group("service")
                  .ReceiveIrcSettingsChange(username, newIrcStatus);
    }

    #endregion
}
