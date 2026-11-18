namespace irai.Features.Requests.Services;

public record Unit;

public record BeatmapNotFound(
    int Id);

public record UserNotFound(
    int? Id);

public record UpstreamError(
    string Details);

public record NotAuthorized;

public record InternalError(string Details);

public union RequestServiceError(BeatmapNotFound, UserNotFound, UpstreamError, NotAuthorized, InternalError);
