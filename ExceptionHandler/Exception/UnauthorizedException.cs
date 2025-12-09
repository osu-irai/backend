namespace osuRequestor.ExceptionHandler.Exception;

public sealed class UnauthorizedException(string info = "") : ApiException(info);