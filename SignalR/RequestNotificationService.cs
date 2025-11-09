using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using osuRequestor.DTO.Responses;
using osuRequestor.Extensions;
using osuRequestor.SignalR.Data;

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
        
        await _hub.Clients.Group($"service").ReceiveFullRequest(request.ToRequest(userId));
    }

    public async Task NotifyAllAsync(string message)
    {
        _logger.LogInformation($"Notifying all users with {message}");
        await _hub.Clients.All.ReceiveGlobalNotification(message);

    }
}