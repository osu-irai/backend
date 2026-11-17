using osuRequestor.Common.Exceptions;

namespace osuRequestor.Features.Requests.Services;

public record Unit;
public record BeatmapNotFound(
    int Id);

public record UserNotFound(
    int? Id);

public record UpstreamError(
    string Details);

public record InternalError;
public union RequestServiceError(BeatmapNotFound, UserNotFound, UpstreamError, InternalError);
