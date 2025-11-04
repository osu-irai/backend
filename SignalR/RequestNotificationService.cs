using Microsoft.AspNetCore.SignalR;
using osuRequestor.DTO.Responses;

namespace osuRequestor.SignalR;

public class RequestNotificationService : IRequestNotificationService
{
    private readonly IHubContext<NotificationHub, INotificationHub> _hub;
    private readonly ILogger<RequestNotificationService> _logger;

    public RequestNotificationService(IHubContext<NotificationHub, INotificationHub> hub, ILogger<RequestNotificationService> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task NotifyUserAsync(int userId, ReceivedRequestResponse request)
    {
        _logger.LogInformation($"Sending request to {userId}");
        await _hub.Clients.Group($"user_{userId}").ReceiveRequest(request);
    }
}