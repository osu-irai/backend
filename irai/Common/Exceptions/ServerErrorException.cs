namespace osuRequestor.Common.Exceptions;

public class ServerErrorException(
    string message) : ApiException(message);
