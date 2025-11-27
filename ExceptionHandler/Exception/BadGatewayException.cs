namespace osuRequestor.ExceptionHandler.Exception;

public sealed class BadGatewayException(string message) : ApiException(message);