using irai.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace irai.Infrastructure.ExceptionHandling;

public class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    #region IExceptionHandler Members

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
                                                CancellationToken cancellationToken)
    {
        if (exception is not ApiException apiException)
        {
            return false;
        }

        switch (apiException)
        {
            case NotFoundException:
                logger.LogWarning("{ApiExceptionMessage} was not found", apiException.Message);
                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                break;
            case ServerErrorException:
                logger.LogError("Internal server error: {ApiExceptionMessage}", apiException.Message);
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                break;
            case BadRequestException:
                logger.LogWarning("Invalid request: {ApiExceptionMessage}", apiException.Message);
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                break;
            case UnauthorizedException:
                logger.LogWarning("User not authorized, {e}", apiException.Message);
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                break;
            case BadGatewayException:
                logger.LogWarning("Upstream error: {ApiExceptionMessage}", apiException.Message);
                httpContext.Response.StatusCode = StatusCodes.Status502BadGateway;
                break;
        }

        await httpContext.Response.WriteAsync(apiException.ResponseMessage, cancellationToken);
        return true;
    }

    #endregion
}
