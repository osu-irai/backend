using MassTransit;
using osuRequestor.DTO.Responses;

namespace osuRequestor.Services.MessageQueue.Contracts;

[EntityName("request-contract")]
public class RequestContract
{
    public required string Target { get; set; }
    public required ReceivedRequestResponse Request { get; set; }
}