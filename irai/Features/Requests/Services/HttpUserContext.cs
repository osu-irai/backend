using System.Diagnostics;
using osuRequestor.Common.Extensions;
using osuRequestor.Features.Requests.Models;

namespace osuRequestor.Features.Requests.Services;

public class HttpUserContext(
    IHttpContextAccessor httpContextAccessor) : IUserContext
{
    #region IUserContext Members

    public int? GetCurrentUserId(PostRequestRequest request)
    {
        return ExtractUserId(request);
    }

    #endregion

    private int? ExtractUserId(PostRequestRequest request)
    {
        if (httpContextAccessor.HttpContext is null)
        {
            throw new UnreachableException();
        }

        if (httpContextAccessor.HttpContext.User.IsInRole("User"))
        {
            return httpContextAccessor.HttpContext.User.Identity.ThrowIfUnauthorized()
                                      .OrOnNullName();
        }

        if (httpContextAccessor.HttpContext.User.IsInRole("Proxy"))
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
