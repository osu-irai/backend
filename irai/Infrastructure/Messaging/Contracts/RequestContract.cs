using irai.Features.Requests.Models;
using MassTransit;

namespace irai.Infrastructure.Messaging.Contracts;

[EntityName("request-contract")]
public class RequestContract
{
    public required string Target { get; set; }
    public required ReceivedRequestResponse Request { get; set; }
}
