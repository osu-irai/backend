using MassTransit;
using osuRequestor.DTO.Requests;
using osuRequestor.Models;

namespace osuRequestor.Services;

public class MessageSubscriberService : IConsumer<PostBaseRequest>
{
    private readonly RequestService _requestService;
    private readonly ILogger<MessageSubscriberService> _logger;

    public MessageSubscriberService(RequestService requestService, ILogger<MessageSubscriberService> logger)
    {
        _requestService = requestService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PostBaseRequest> context)
    {
        _logger.LogInformation("Received a message from RMQ");
        ArgumentNullException.ThrowIfNull(context.Message.DestinationId);
        ArgumentNullException.ThrowIfNull(context.Message.BeatmapId);
        var result = await _requestService.CreateRequest(null, context.Message.DestinationId.Value, context.Message.BeatmapId.Value,
            RequestSource.Twitch);

        if (result.IsErr)
        {
            _logger.LogWarning("Failed to create a request from Twitch: {Error}", result.Value);
        }
    }
}