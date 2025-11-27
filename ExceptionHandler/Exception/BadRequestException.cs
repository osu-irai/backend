namespace osuRequestor.ExceptionHandler.Exception;

public class BadRequestException(string message) : ApiException(message);