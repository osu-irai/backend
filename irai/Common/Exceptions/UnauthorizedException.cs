namespace osuRequestor.Common.Exceptions;

public sealed class UnauthorizedException(
    string info = "") : ApiException(info);
