using irai.Features.Requests.Models;

namespace irai.Infrastructure.Notifications;

public interface IRequestNotificationService
{
    Task NotifyUserAsync(string destinationUsername, ReceivedRequestResponse request);

    Task NotifyAllAsync(string message);

    Task NotifyAboutIrcChange(string username, bool newIrcStatus);
}
