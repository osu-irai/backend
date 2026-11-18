namespace irai.Common.Exceptions;

public sealed class BadGatewayException(
    string message) : ApiException(message);
