using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using osuRequestor.Common.Controllers;
using osuRequestor.Common.Exceptions;

namespace osuRequestor.Tests.Common;

[TestFixture]
public class CrudControllerTests
{
    private static TestCrudController CreateController(AuthenticateResult result)
    {
        var authentication = Substitute.For<IAuthenticationService>();
        authentication
            .AuthenticateAsync(Arg.Any<HttpContext>(), Arg.Any<string>())
            .Returns(result);

        var services = new ServiceCollection()
                       .AddSingleton(authentication)
                       .BuildServiceProvider();

        return new TestCrudController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = services }
            }
        };
    }

    private static AuthenticationTicket Ticket(params Claim[] claims)
    {
        return new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(claims, "InternalCookies")),
            "InternalCookies");
    }

    private sealed class TestCrudController : CrudController
    {
        public Task<int> GetOsuClaimForTest()
        {
            return GetOsuClaim();
        }

        public Task TryAuthenticateOsuForTest()
        {
            return TryAuthenticateOsu();
        }

        public Task TryAuthenticateTwitchForTest()
        {
            return TryAuthenticateTwitch();
        }
    }

    [Test]
    public async Task GetOsuClaim_WhenAuthenticated_ReturnsParsedUserId()
    {
        var controller = CreateController(AuthenticateResult.Success(
                                              Ticket(new Claim(ClaimTypes.Name, "123"))));

        var claim = await controller.GetOsuClaimForTest();

        Assert.That(claim, Is.EqualTo(123));
    }

    [Test]
    public void GetOsuClaim_WhenAuthenticationFails_ThrowsUnauthorized()
    {
        var controller = CreateController(AuthenticateResult.Fail("nope"));

        Assert.ThrowsAsync<UnauthorizedException>(controller.GetOsuClaimForTest);
    }

    [Test]
    public void GetOsuClaim_WhenNameIsEmpty_ThrowsUnauthorized()
    {
        var controller = CreateController(AuthenticateResult.Success(
                                              Ticket(new Claim(ClaimTypes.Name, ""))));

        Assert.ThrowsAsync<UnauthorizedException>(controller.GetOsuClaimForTest);
    }

    [Test]
    public void GetOsuClaim_WhenNameIsMissing_ThrowsUnauthorized()
    {
        var controller = CreateController(AuthenticateResult.Success(Ticket()));

        Assert.ThrowsAsync<UnauthorizedException>(controller.GetOsuClaimForTest);
    }

    [Test]
    public void GetOsuClaim_WhenNameIsNotNumeric_ThrowsFormatException()
    {
        var controller = CreateController(AuthenticateResult.Success(
                                              Ticket(new Claim(ClaimTypes.Name, "alice"))));

        Assert.ThrowsAsync<FormatException>(controller.GetOsuClaimForTest);
    }

    [Test]
    public async Task TryAuthenticateOsu_WhenAuthenticated_Completes()
    {
        var controller = CreateController(AuthenticateResult.Success(Ticket()));

        await controller.TryAuthenticateOsuForTest();
    }

    [Test]
    public void TryAuthenticateOsu_WhenAuthenticationFails_ThrowsUnauthorized()
    {
        var controller = CreateController(AuthenticateResult.Fail("nope"));

        Assert.ThrowsAsync<UnauthorizedException>(controller.TryAuthenticateOsuForTest);
    }

    [Test]
    public async Task TryAuthenticateTwitch_WhenAuthenticated_Completes()
    {
        var controller = CreateController(AuthenticateResult.Success(Ticket()));

        await controller.TryAuthenticateTwitchForTest();
    }
}
