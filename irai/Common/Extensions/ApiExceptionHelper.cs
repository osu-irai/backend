using System.Diagnostics;
using System.Security.Principal;
using osu.NET;
using osuRequestor.Common.Exceptions;
using osuRequestor.Features.Requests.Services;

namespace osuRequestor.Common.Extensions;

public static class ApiExceptionHelper
{
    public static async Task<T> ThrowIfUnauthorized<T>(this Task<Result<T, RequestServiceError>> value) where T : notnull
    {
        return (await value) switch
        {
            OkVariant<T> okVariant => okVariant.Value,
            ErrorVariant<RequestServiceError> { Value: BeatmapNotFound val } =>
                throw new UnreachableException(),
            ErrorVariant<RequestServiceError> { Value: UserNotFound val } =>
                throw new UnauthorizedException(),
            ErrorVariant<RequestServiceError> { Value: UpstreamError val } =>
                throw new BadGatewayException(val.Details),
            ErrorVariant<RequestServiceError> { Value: InternalError } => throw new ServerErrorException("Internal"),
        };
    }

    public static async Task<T> ThrowIfRequestError<T>(this Task<Result<T, RequestServiceError>> value)
        where T : notnull
    {
        return (await value) switch
        {
            ErrorVariant<RequestServiceError> { Value: BeatmapNotFound val } =>
                throw new NotFoundException($"Beatmap {val.Id}"),
            ErrorVariant<RequestServiceError> { Value: UserNotFound val } =>
                throw new NotFoundException($"User {val.Id}"),
            ErrorVariant<RequestServiceError> { Value: UpstreamError val } =>
                throw new ServerErrorException(val.Details),
            ErrorVariant<RequestServiceError> { Value: InternalError } => throw new ServerErrorException("Internal"),
            OkVariant<T> okVariant                                     => okVariant.Value,
        };
    }

    public static void OrNotFound<T>(this T value)
    {
        throw new NotFoundException($"{typeof(T)}");
    }

    public static T OrBadRequest<T>(this T? value, string message)
    {
        return value ?? throw new BadRequestException(message);
    }

    public static async Task<T?> BadGatewayOnFailure<T>(this Task<ApiResult<T>> value, string message) where T : class
    {
        var val = await value;
        if (val.IsFailure)
        {
            throw new BadGatewayException(message);
        }

        return val.Value;
    }

    public static async Task<T?> BadGatewayOnFailure<T>(this Task<ApiResult<T>> value) where T : class
    {
        return await value.BadGatewayOnFailure(value.Result.Error?.Message ?? "");
    }

    public static async Task<T> OrNotFound<T>(this Task<T?> value) where T : class
    {
        var val = await value;
        if (val is null)
        {
            throw new NotFoundException($"{typeof(T)} not found");
        }

        return val;
    }

    public static async Task<T?> OrBadRequest<T>(this Task<T?> value) where T : class
    {
        var val = await value;
        if (val is null)
        {
            throw new BadRequestException($"{typeof(T)} not found");
        }

        return val;
    }

    public static async Task<T?> OrBadGateway<T>(this Task<ApiResult<T>> value, string message) where T : class
    {
        var val = await value;
        if (val.IsFailure)
        {
            throw new BadGatewayException($"Upstream error with {message}");
        }

        return val.Value;
    }

    public static IIdentity ThrowIfUnauthorized(this IIdentity? identity)
    {
        return identity ?? throw new UnauthorizedException();
    }

    public static int OrOnNullName(this IIdentity identity)
    {
        return identity.Name is null ? throw new UnauthorizedException() : int.Parse(identity.Name);
    }
}
