namespace osuRequestor.ExceptionHandler.Exception;

public abstract class ApiException : System.Exception
{
    internal string ResponseMessage { get; set; }
    public ApiException(string message) : base(message)
    {
        ResponseMessage = message;
    }
} 