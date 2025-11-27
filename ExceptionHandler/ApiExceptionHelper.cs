using System.Diagnostics;
using System.Security.Principal;
using osu.NET;
using osuRequestor.ExceptionHandler.Exception;
using osuRequestor.Services;

namespace osuRequestor.Exceptions;

public static class ApiExceptionHelper
{
    public static async Task<T> ThrowIfUnauthorized<T>(this Task<RequestServiceResult<T>> value)
    {
        return (await value).Match(
            val => val,
            bnf => throw new UnreachableException(),
            unf => throw new UnauthorizedException(),
            upstream => throw new BadGatewayException(upstream.Details),
            inter => throw new ServerErrorException("Internal error"));
    }

    public static async Task<T> ThrowIfNotFound<T>(this Task<RequestServiceResult<T>> value)
    {
        return (await value).Match(
            val => val,
            bnf => throw new NotFoundException("Beatmap"),
            unf => throw new NotFoundException("User"),
            upstream => throw new BadGatewayException(upstream.Details),
            inter => throw new ServerErrorException("Internal error"));
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
        if (val.IsFailure) throw new BadGatewayException(message);
        return val.Value;
    }

    public static async Task<T?> BadGatewayOnFailure<T>(this Task<ApiResult<T>> value) where T : class
    {
        return await value.BadGatewayOnFailure(value.Result.Error?.Message ?? "");
    }

    public static async Task<T> OrNotFound<T>(this Task<T?> value) where T : class
    {
        var val = await value;
        if (val is null) throw new NotFoundException($"{typeof(T)} not found");
        return val;
    }

    public static async Task<T?> OrBadRequest<T>(this Task<T?> value) where T : class
    {
        var val = await value;
        if (val is null) throw new BadRequestException($"{typeof(T)} not found");
        return val;
    }

    public static async Task<T?> OrBadGateway<T>(this Task<ApiResult<T>> value, string message) where T : class
    {
        var val = await value;
        if (val.IsFailure) throw new BadGatewayException($"Upstream error with {message}");

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