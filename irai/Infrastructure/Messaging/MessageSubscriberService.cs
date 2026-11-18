using irai.Common.Exceptions;
using irai.Features.Requests.Models;
using irai.Features.Requests.Persistence;
using irai.Features.Requests.Services;
using MassTransit;

namespace irai.Infrastructure.Messaging;

public class MessageSubscriberService(
    ILogger<MessageSubscriberService> logger,
    IRequestService requestService
) : IConsumer<PostBaseRequest>
{
    #region IConsumer<PostBaseRequest> Members

    public async Task Consume(ConsumeContext<PostBaseRequest> context)
    {
        logger.LogInformation("Received a message from RMQ");
        ArgumentNullException.ThrowIfNull(context.Message.DestinationId);
        ArgumentNullException.ThrowIfNull(context.Message.BeatmapId);
        try
        {
            var result = await requestService.CreateRequest(null, context.Message.DestinationId.Value,
                                                            context.Message.BeatmapId.Value,
                                                            RequestSource.Twitch);

            if (result is ErrorVariant<RequestServiceError> { Value: var error })
            {
                logger.LogWarning("Failed to create a request from Twitch: {Error}", error);
            }
        }
        // TODO: This should probably be error-as-value'd
        catch (NotFoundException exception)
        {
            logger.LogWarning(exception, "Dropping Twitch request for unknown user or beatmap");
        }
        catch (UnauthorizedException exception)
        {
            logger.LogWarning(exception, "Dropping Twitch request, no user token available");
        }
        catch (ServerErrorException exception)
        {
            logger.LogWarning(exception, "Dropping Twitch request after upstream failure");
        }
    }

    #endregion
}
