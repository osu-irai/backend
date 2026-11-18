using irai.Features.Requests.Models;

namespace irai.Infrastructure.Notifications;

public interface IRequestNotificationService
{
    Task NotifyUserAsync(string destinationUsername, ReceivedRequestResponse request);

    Task NotifyAboutIrcChange(string username, bool newIrcStatus);
}
