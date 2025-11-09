using osuRequestor.DTO.Responses;

namespace osuRequestor.SignalR.Data;

public class RequestWithTarget
{
    public int Target { get; set; }
    public required ReceivedRequestResponse Request { get; set; }
};