using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using osuRequestor.Common.Exceptions;
using osuRequestor.Features.Requests.Models;
using osuRequestor.Features.Requests.Services;

namespace osuRequestor.Tests.Features.Requests;

/// <summary>
/// User ID extraction from auth context
/// </summary>
[TestFixture]
public class HttpUserContextTests
{
    private static HttpUserContext CreateContext(string name, string role)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Role, role)
            ],
            "test");

        return new HttpUserContext(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        });
    }

    [Test]
    public void GetCurrentUserId_WhenHttpContextIsMissing_ThrowsUnreachable()
    {
        var context = new HttpUserContext(new HttpContextAccessor());

        Assert.Throws<UnreachableException>(() => context.GetCurrentUserId(new PostRequestRequest()));
    }

    [Test]
    public void GetCurrentUserId_WhenProxyHasNoSourceId_ThrowsBadRequest()
    {
        var context = CreateContext("42", "Proxy");

        var exception = Assert.Throws<BadRequestException>(() => context.GetCurrentUserId(new PostRequestRequest()));

        Assert.That(exception!.Message, Is.EqualTo("Proxy user has not provided source user ID"));
    }

    [Test]
    public void GetCurrentUserId_WhenProxyHasSourceId_ReturnsSourceId()
    {
        var context = CreateContext("42", "Proxy");
        var request = new PostRequestRequest { SourceId = 7 };

        var id = context.GetCurrentUserId(request);

        Assert.That(id, Is.EqualTo(7));
    }

    [Test]
    public void GetCurrentUserId_WhenUserHasNoKnownRole_ReturnsNull()
    {
        var context = CreateContext("42", "Bot");

        var id = context.GetCurrentUserId(new PostRequestRequest());

        Assert.That(id, Is.Null);
    }

    [Test]
    public void GetCurrentUserId_WhenUserRoleHasNumericName_ReturnsUserId()
    {
        var context = CreateContext("42", "User");

        var id = context.GetCurrentUserId(new PostRequestRequest());

        Assert.That(id, Is.EqualTo(42));
    }

    [Test]
    public void GetCurrentUserId_WhenUserRoleNameIsNotNumeric_ThrowsFormatException()
    {
        var context = CreateContext("alice", "User");

        Assert.Throws<FormatException>(() => context.GetCurrentUserId(new PostRequestRequest()));
    }
}
