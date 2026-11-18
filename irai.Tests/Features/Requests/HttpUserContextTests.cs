using System.Diagnostics;
using System.Security.Claims;
using irai.Common.Constants;
using irai.Common.Exceptions;
using irai.Features.Requests.Models;
using irai.Features.Requests.Services;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace irai.Tests.Features.Requests;

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
        var context = CreateContext("42", Roles.PROXY);

        var exception = Assert.Throws<BadRequestException>(() => context.GetCurrentUserId(new PostRequestRequest()));

        Assert.That(exception!.Message, Is.EqualTo("Proxy user has not provided source user ID"));
    }

    [Test]
    public void GetCurrentUserId_WhenProxyHasSourceId_ReturnsSourceId()
    {
        var context = CreateContext("42", Roles.PROXY);
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
        var context = CreateContext("42", Roles.USER);

        var id = context.GetCurrentUserId(new PostRequestRequest());

        Assert.That(id, Is.EqualTo(42));
    }

    [Test]
    public void GetCurrentUserId_WhenUserRoleNameIsNotNumeric_ThrowsFormatException()
    {
        var context = CreateContext("alice", Roles.USER);

        Assert.Throws<FormatException>(() => context.GetCurrentUserId(new PostRequestRequest()));
    }
}
