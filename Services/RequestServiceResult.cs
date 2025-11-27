using OneOf;
using osuRequestor.ExceptionHandler.Exception;

namespace osuRequestor.Services;

[GenerateOneOf]
public partial class RequestServiceResult<T> : OneOfBase<T, BeatmapNotFound, UserNotFound, UpstreamError, InternalError>
{
    public bool IsOk => IsT0;
    public bool IsErr => !IsT0;

    public T ThrowOnError()
    {
        return Match(
            val => val,
            bnf => throw new NotFoundException($"Beatmap {bnf.Id}"),
            unf => throw new NotFoundException($"User {unf.Id}"),
            upstream => throw new BadGatewayException($"Upstream error: {upstream.Details}"),
            downstream => throw new ServerErrorException("Internal server error")
        );
    }
}

public record Ok;

public record BeatmapNotFound(int Id);

public record UserNotFound(int? Id);

public record UpstreamError(string Details);

public record InternalError;