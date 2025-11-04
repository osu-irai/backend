using osuRequestor.DTO.Responses;
using osuRequestor.Models;

namespace osuRequestor.SignalR;

public interface IRequestNotificationService
{
    Task NotifyUserAsync(int userId, ReceivedRequestResponse request);
}