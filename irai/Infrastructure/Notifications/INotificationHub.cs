using irai.Features.Requests.Models;
using irai.Infrastructure.Notifications.Data;

namespace irai.Infrastructure.Notifications;

public interface INotificationHub
{
    Task ReceiveRequest(ReceivedRequestResponse request);

    Task ReceiveFullRequest(RequestWithTarget request);

    Task ReceiveGlobalNotification(string notification);

    Task ReceiveIrcSettingsChange(string username, bool newIrcState);

    Task ReceiveTwitchSettingsChange(string username, TwitchData twitchData);
}
