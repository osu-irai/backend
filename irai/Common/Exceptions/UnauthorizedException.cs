namespace irai.Common.Exceptions;

public sealed class UnauthorizedException(
    string info = "") : ApiException(info);
