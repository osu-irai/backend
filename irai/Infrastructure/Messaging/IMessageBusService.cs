using irai.Features.Requests.Models;

namespace irai.Infrastructure.Messaging;

public interface IMessageBusService
{
    Task SubmitRequestAsync(string target, ReceivedRequestResponse request);
    Task SubmitIrcSettingsChangeAsync(string username, bool isEnabled);
    Task SubmitTwitchSettingsChangeAsync(string twitchUserId, int osuId, bool isEnabled);
}
