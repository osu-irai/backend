using MassTransit;
using osuRequestor.Features.Requests.Models;
using osuRequestor.Infrastructure.Messaging.Contracts;

namespace osuRequestor.Infrastructure.Messaging;

public class MessageBusService : IMessageBusService
{
    private readonly ISendEndpointProvider _endpointProvider;
    private readonly ILogger<MessageBusService> _logger;
    private readonly IPublishEndpoint _publish;

    public MessageBusService(IPublishEndpoint publish, ISendEndpointProvider endpointProvider,
                             ILogger<MessageBusService> logger)
    {
        this._publish          = publish;
        this._endpointProvider = endpointProvider;
        this._logger           = logger;
    }

    public Task SubmitRequestAsync(string target, ReceivedRequestResponse request)
    {
        return this._publish.Publish(new RequestContract
        {
            Target  = target,
            Request = request
        });
    }

    public async Task SubmitIrcSettingsChangeAsync(string username, bool isEnabled)
    {
        var endpoint = await this._endpointProvider.GetSendEndpoint(new Uri("queue:irc-settings"));
        this._logger.LogInformation("Changing IRC settings of {Username}", username);
        await endpoint.Send(new IrcSettingsChangeContract { Username = username, IsEnabled = isEnabled });
    }

    public async Task SubmitTwitchSettingsChangeAsync(string twitchUserId, int osuId, bool isEnabled)
    {
        var endpoint = await this._endpointProvider.GetSendEndpoint(new Uri("queue:twitch-settings"));
        this._logger.LogInformation("Changing Twitch settings of {Username}, enabled: {isEnabled}", twitchUserId,
                                    isEnabled);
        await endpoint.Send(new TwitchSettingsChangeContract
                                { TwitchUserId = twitchUserId, OsuId = osuId, IsEnabled = isEnabled });
    }
}
