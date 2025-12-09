using osuRequestor.Apis.TwitchApi.Models;
using osuRequestor.DTO.Responses;
using osuRequestor.SignalR.Data;

namespace osuRequestor.SignalR;

public interface INotificationHub
{
    Task ReceiveRequest(ReceivedRequestResponse request);

    Task ReceiveFullRequest(RequestWithTarget request);

    Task ReceiveGlobalNotification(string notification);

    Task ReceiveIrcSettingsChange(string username, bool newIrcState);

    Task ReceiveTwitchSettingsChange(string username, TwitchData twitchData);
}