using Microsoft.AspNetCore.Diagnostics;
using osuRequestor.ExceptionHandler.Exception;

namespace osuRequestor.Exceptions;

public class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ApiException apiException)
        {
            return ValueTask.FromResult(false);
        }

        switch (apiException)
        {
            case NotFoundException:
                _logger.LogWarning("{ApiExceptionMessage} was not found", apiException.Message);
                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                break;
            case ServerErrorException:
                _logger.LogError("Internal server error: {ApiExceptionMessage}", apiException.Message);
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                break;
            case BadRequestException:
                _logger.LogWarning("Invalid request: {ApiExceptionMessage}", apiException.Message);
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                break;
            case UnauthorizedException:
                _logger.LogWarning("User not authorized");
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                break;
            case BadGatewayException:
                _logger.LogWarning("Upstream error: {ApiExceptionMessage}", apiException.Message);
                httpContext.Response.StatusCode = StatusCodes.Status502BadGateway;
                break;
        }


        return ValueTask.FromResult(true);
    }
}