namespace osuRequestor.Common.Exceptions;

public abstract class ApiException(
    string message) : Exception(message)
{
    internal string ResponseMessage { get; set; } = message;
}
