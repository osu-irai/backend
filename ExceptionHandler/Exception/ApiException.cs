namespace osuRequestor.ExceptionHandler.Exception;

public abstract class ApiException(string message) : System.Exception(message)
{
    internal string ResponseMessage { get; set; } = message;
}