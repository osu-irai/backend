namespace osuRequestor.ExceptionHandler.Exception;

public class NotFoundException(string message) : ApiException(message);