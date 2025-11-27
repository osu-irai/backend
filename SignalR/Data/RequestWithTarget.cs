using osuRequestor.DTO.Responses;

namespace osuRequestor.SignalR.Data;

public class RequestWithTarget
{
    public required string Target { get; set; }
    public required ReceivedRequestResponse Request { get; set; }
}