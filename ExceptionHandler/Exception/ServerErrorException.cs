namespace osuRequestor.ExceptionHandler.Exception;

public class ServerErrorException(string message) : ApiException(message);