using osuRequestor.DTO.Responses;

namespace osuRequestor.SignalR;

public interface INotificationHub
{
    Task ReceiveRequest(ReceivedRequestResponse request);
}