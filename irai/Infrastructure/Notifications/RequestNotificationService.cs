using irai.Features.Requests.Mappings;
using irai.Features.Requests.Models;
using Microsoft.AspNetCore.SignalR;

namespace irai.Infrastructure.Notifications;

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
        this._logger.LogInformation("Sending request {RequestId} to {Username}", request.Id, destinationUsername);
        await this._hub.Clients.Group($"user_{destinationUsername}")
                  .ReceiveRequest(request);

        await this._hub.Clients.Group("service")
                  .ReceiveFullRequest(request.ToRequest(destinationUsername));
    }

    public async Task NotifyAboutIrcChange(string username, bool newIrcStatus)
    {
        this._logger.LogInformation("User {Username} toggled IRC to {Status}", username, newIrcStatus);
        await this._hub.Clients.Group("service")
                  .ReceiveIrcSettingsChange(username, newIrcStatus);
    }

    #endregion
}
