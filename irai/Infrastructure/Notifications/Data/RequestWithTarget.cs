using irai.Features.Requests.Models;

namespace irai.Infrastructure.Notifications.Data;

public class RequestWithTarget
{
    public required string Target { get; set; }
    public required ReceivedRequestResponse Request { get; set; }
}
