using osuRequestor.Exceptions;

namespace osuRequestor.ExceptionHandler.Exception;

public class BadRequestException : ApiException
{
    public BadRequestException(string message) : base(message)
    {
    }
}