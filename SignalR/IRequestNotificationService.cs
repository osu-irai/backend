using osuRequestor.DTO.Responses;
using osuRequestor.Models;

namespace osuRequestor.SignalR;

public interface IRequestNotificationService
{
    Task NotifyUserAsync(string destinationUsername, ReceivedRequestResponse request);

    Task NotifyAllAsync(string message);

    Task NotifyAboutIrcChange(string username, bool newIrcStatus);
}