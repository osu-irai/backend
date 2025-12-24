using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace osuRequestor.Tests.Common;

internal static class TestControllerContext
{
    public static void SignIn(ControllerBase controller, int userId, string role = "User")
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            ],
            "InternalCookies");

        var ticket         = new AuthenticationTicket(new ClaimsPrincipal(identity), "InternalCookies");
        var authentication = Substitute.For<IAuthenticationService>();
        authentication
            .AuthenticateAsync(Arg.Any<HttpContext>(), Arg.Any<string>())
            .Returns(AuthenticateResult.Success(ticket));

        var services = new ServiceCollection()
                       .AddSingleton(authentication)
                       .BuildServiceProvider();

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = services }
        };
    }
}
