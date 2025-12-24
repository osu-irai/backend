namespace osuRequestor.Common.Exceptions;

public class NotFoundException(
    string message) : ApiException(message);
