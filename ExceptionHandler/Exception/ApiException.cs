namespace osuRequestor.ExceptionHandler.Exception;

public abstract class ApiException : System.Exception
{
    public ApiException(string message) : base(message) {}
} 