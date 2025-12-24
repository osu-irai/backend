using osuRequestor.Features.Requests.Models;

namespace osuRequestor.Infrastructure.Messaging;

public interface IMessageBusService
{
    Task SubmitRequestAsync(string target, ReceivedRequestResponse request);
    Task SubmitIrcSettingsChangeAsync(string username, bool isEnabled);
    Task SubmitTwitchSettingsChangeAsync(string twitchUserId, int osuId, bool isEnabled);
}
