namespace irai.Common.Exceptions;

public class NotFoundException(
    string message) : ApiException(message);
