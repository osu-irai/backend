using System.Diagnostics;
using System.Security.Claims;
using osuRequestor.DTO.Requests;
using osuRequestor.ExceptionHandler.Exception;
using osuRequestor.Exceptions;

namespace osuRequestor.Services;

public class HttpUserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? GetCurrentUserId(PostRequestRequest request) => ExtractUserId(request);

    private int? ExtractUserId(PostRequestRequest request)
    {
        if (_httpContextAccessor.HttpContext is null) throw new UnreachableException();
        if (_httpContextAccessor.HttpContext.User.IsInRole("User"))
        {
           return _httpContextAccessor.HttpContext.User.Identity.ThrowIfUnauthorized().OrOnNullName();
        }

        if (_httpContextAccessor.HttpContext.User.IsInRole("Proxy"))
        {
            return request.SourceId.OrBadRequest("Proxy user has not provided source user ID");
        }
        return null;
    }
}

public enum UserType
{
    User,
    Proxy,
    Bot
}