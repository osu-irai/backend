using osuRequestor.Features.Requests.Models;

namespace osuRequestor.Infrastructure.Notifications.Data;

public class RequestWithTarget
{
    public required string Target { get; set; }
    public required ReceivedRequestResponse Request { get; set; }
}
