using irai.Features.Requests.Models;
using irai.Infrastructure.Messaging.Contracts;
using MassTransit;

namespace irai.Infrastructure.Messaging;

public class MessageBusService(IPublishEndpoint publish, ISendEndpointProvider endpointProvider,
                               ILogger<MessageBusService> logger) : IMessageBusService
{
    #region IMessageBusService Members

    public Task SubmitRequestAsync(string target, ReceivedRequestResponse request)
    {
        return publish.Publish(new RequestContract
        {
            Target  = target,
            Request = request
        });
    }

    public async Task SubmitIrcSettingsChangeAsync(string username, bool isEnabled)
    {
        var endpoint = await endpointProvider.GetSendEndpoint(new Uri("queue:irc-settings"));
        logger.LogInformation("Changing IRC settings of {Username}", username);
        await endpoint.Send(new IrcSettingsChangeContract { Username = username, IsEnabled = isEnabled });
    }

    public async Task SubmitTwitchSettingsChangeAsync(string twitchUserId, int osuId, bool isEnabled)
    {
        var endpoint = await endpointProvider.GetSendEndpoint(new Uri("queue:twitch-settings"));
        logger.LogInformation("Changing Twitch settings of {Username}, enabled: {isEnabled}", twitchUserId,
                                    isEnabled);
        await endpoint.Send(new TwitchSettingsChangeContract
                                { TwitchUserId = twitchUserId, OsuId = osuId, IsEnabled = isEnabled });
    }

    #endregion
}
