namespace osuRequestor.ExceptionHandler.Exception;

public sealed class BadGatewayException : ApiException
{
    public BadGatewayException(string message) : base(message)
    {
    }
}