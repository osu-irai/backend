using osuRequestor.Exceptions;

namespace osuRequestor.ExceptionHandler.Exception;

public class ServerErrorException : ApiException
{
    public ServerErrorException(string message) : base(message)
    {
    }
}