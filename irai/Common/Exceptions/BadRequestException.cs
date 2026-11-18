namespace irai.Common.Exceptions;

public class BadRequestException(
    string message) : ApiException(message);
