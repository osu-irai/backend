namespace osuRequestor.Common.Exceptions;

public class BadRequestException(
    string message) : ApiException(message);
