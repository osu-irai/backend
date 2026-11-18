using System.Security.Principal;
using ChiffadicErrors;
using irai.Common.Exceptions;
using irai.Features.Requests.Services;
using osu.NET;

namespace irai.Common.Extensions;

public static class ApiExceptionHelper
{
    public static async Task<T> ThrowIfRequestError<T>(this Task<Result<T, RequestServiceError>> value)
        where T : notnull
    {
        return await value switch
        {
            Error<RequestServiceError> { Value: BeatmapNotFound val } =>
                throw new NotFoundException($"Beatmap {val.Id}"),
            Error<RequestServiceError> { Value: UserNotFound val } =>
                throw new NotFoundException($"User {val.Id}"),
            Error<RequestServiceError> { Value: UpstreamError val } =>
                throw new ServerErrorException(val.Details),
            Error<RequestServiceError> { Value: NotAuthorized } =>
                throw new UnauthorizedException(),
            Error<RequestServiceError> { Value: InternalError } => throw new ServerErrorException("Internal"),
            Ok<T> Ok                                     => Ok.Value
        };
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

    public static IIdentity ThrowIfUnauthorized(this IIdentity? identity)
    {
        return identity ?? throw new UnauthorizedException();
    }

    public static int OrOnNullName(this IIdentity identity)
    {
        return identity.Name is null ? throw new UnauthorizedException() : int.Parse(identity.Name);
    }
}
