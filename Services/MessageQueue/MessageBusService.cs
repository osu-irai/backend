using MassTransit;
using osuRequestor.DTO.Responses;
using osuRequestor.Services.MessageQueue.Contracts;

namespace osuRequestor.Services;

public class MessageBusService
{
    private readonly IPublishEndpoint _publish;
    private readonly ISendEndpointProvider _endpointProvider;
    private readonly ILogger<MessageBusService> _logger;

    public MessageBusService(IPublishEndpoint publish, ISendEndpointProvider endpointProvider, ILogger<MessageBusService> logger)
    {
        _publish = publish;
        _endpointProvider = endpointProvider;
        _logger = logger;
    }

    public Task SubmitRequestAsync(string target, ReceivedRequestResponse request) =>
        _publish.Publish(new RequestContract
        {
            Target = target,
            Request = request
        });

    public async Task SubmitIrcSettingsChangeAsync(string username, bool isEnabled)
    {
        var endpoint = await _endpointProvider.GetSendEndpoint(new Uri("queue:irc-settings"));
        _logger.LogInformation("Changing IRC settings of {Username}", username);
        await endpoint.Send(new IrcSettingsChangeContract { Username = username, IsEnabled = isEnabled });
    }

    public async Task SubmitTwitchSettingsChangeAsync(string twitchUserId, int osuId, bool isEnabled)
    {
        var endpoint = await _endpointProvider.GetSendEndpoint(new Uri("queue:twitch-settings"));
        _logger.LogInformation("Changing Twitch settings of {Username}, enabled: {isEnabled}", twitchUserId, isEnabled);
        await endpoint.Send(new TwitchSettingsChangeContract { TwitchUserId = twitchUserId, OsuId = osuId, IsEnabled = isEnabled});
    }
}