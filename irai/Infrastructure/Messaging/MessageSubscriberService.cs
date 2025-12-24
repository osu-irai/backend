using MassTransit;
using osuRequestor.Features.Requests.Models;
using osuRequestor.Features.Requests.Services;
using osuRequestor.Infrastructure.Persistence.Models;

namespace osuRequestor.Infrastructure.Messaging;

public class MessageSubscriberService : IConsumer<PostBaseRequest>
{
    private readonly ILogger<MessageSubscriberService> _logger;
    private readonly IRequestService _requestService;

    public MessageSubscriberService(IRequestService requestService, ILogger<MessageSubscriberService> logger)
    {
        this._requestService = requestService;
        this._logger         = logger;
    }

    #region IConsumer<PostBaseRequest> Members

    public async Task Consume(ConsumeContext<PostBaseRequest> context)
    {
        this._logger.LogInformation("Received a message from RMQ");
        ArgumentNullException.ThrowIfNull(context.Message.DestinationId);
        ArgumentNullException.ThrowIfNull(context.Message.BeatmapId);
        var result = await this._requestService.CreateRequest(null, context.Message.DestinationId.Value,
                                                              context.Message.BeatmapId.Value,
                                                              RequestSource.Twitch);

        if (result.IsErr)
        {
            this._logger.LogWarning("Failed to create a request from Twitch: {Error}", result.Value);
        }
    }

    #endregion
}
