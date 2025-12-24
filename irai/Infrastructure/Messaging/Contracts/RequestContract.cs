using MassTransit;
using osuRequestor.Features.Requests.Models;

namespace osuRequestor.Infrastructure.Messaging.Contracts;

[EntityName("request-contract")]
public class RequestContract
{
    public required string Target { get; set; }
    public required ReceivedRequestResponse Request { get; set; }
}
