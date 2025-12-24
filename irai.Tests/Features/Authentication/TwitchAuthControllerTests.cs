using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using osuRequestor.Features.Authentication.Controllers;
using osuRequestor.Infrastructure.Configuration;
using osuRequestor.Infrastructure.ExternalServices.TwitchApi;
using osuRequestor.Infrastructure.Persistence;

namespace osuRequestor.Tests.Features.Authentication;

[TestFixture]
public class TwitchAuthControllerTests
{
    private static AuthenticationTicket Ticket(string scheme, int? userId = null,
                                               params (string Name, string Value)[] tokens)
    {
        var claims = userId is null
                         ? Array.Empty<Claim>()
                         : [new Claim(ClaimTypes.Name, userId.Value.ToString())];
        var identity   = new ClaimsIdentity(claims, scheme);
        var properties = new AuthenticationProperties();
        properties.StoreTokens(tokens.Select(t => new AuthenticationToken { Name = t.Name, Value = t.Value }));
        return new AuthenticationTicket(new ClaimsPrincipal(identity), properties, scheme);
    }

    private static Harness CreateHarness()
    {
        var db             = CreateDatabase();
        var authentication = Substitute.For<IAuthenticationService>();
        var twitch = new TwitchApiProvider(
            Options.Create(new TwitchConfig
            {
                ClientId     = "client",
                ClientSecret = "secret",
                RedirectUrl  = "/callback"
            }),
            new HttpClient(),
            NullLogger<TwitchApiProvider>.Instance);

        var controller = new TwitchAuthController(
            NullLogger<TwitchAuthController>.Instance,
            new global::osuRequestor.Features.Authentication.Services.AuthenticationService(
                db, NullLogger<global::osuRequestor.Features.Authentication.Services.AuthenticationService>.Instance),
            twitch,
            Options.Create(new ServerConfig { HomePage = "/home" }));

        var services = new ServiceCollection()
                       .AddSingleton(authentication)
                       .AddSingleton<IUrlHelperFactory, UrlHelperFactory>()
                       .BuildServiceProvider();
        var routeData = new RouteData();
        routeData.Routers.Add(Substitute.For<IRouter>());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = services },
            RouteData   = routeData,
            ActionDescriptor = new ControllerActionDescriptor
            {
                ControllerName = "TwitchAuth",
                ActionName     = "Login"
            }
        };

        return new Harness(db, controller, authentication);
    }

    private static DatabaseContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
                      .UseInMemoryDatabase(Guid.NewGuid()
                                               .ToString())
                      .Options;

        return new DatabaseContext(options);
    }

    private sealed record Harness(
        DatabaseContext Db,
        TwitchAuthController Controller,
        IAuthenticationService Authentication);

    /// <summary>
    /// Authorization failure
    /// </summary>
    [Test]
    public async Task CompleteAuthentication_WhenNotAuthenticated_ThrowsUnauthorized()
    {
        var             harness = CreateHarness();
        await using var db      = harness.Db;
        harness.Authentication
               .AuthenticateAsync(Arg.Any<HttpContext>(), "Twitch")
               .Returns(AuthenticateResult.NoResult());

        var result = await harness.Controller.CompleteAuthentication();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    /// <summary>
    /// Token auth failure
    /// </summary>
    [Test]
    public async Task CompleteAuthentication_WhenTokensAreMissing_ReturnsUnauthorizedResult()
    {
        var             harness      = CreateHarness();
        await using var db           = harness.Db;
        var             twitchTicket = Ticket("Twitch");
        harness.Authentication
               .AuthenticateAsync(Arg.Any<HttpContext>(), "Twitch")
               .Returns(AuthenticateResult.Success(twitchTicket));
        harness.Authentication
               .AuthenticateAsync(Arg.Any<HttpContext>(), "InternalCookies")
               .Returns(AuthenticateResult.Success(Ticket("InternalCookies", 1)));

        var result = await harness.Controller.CompleteAuthentication();

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    /// <summary>
    /// Happy path
    /// </summary>
    [Test]
    public void Login_ReturnsChallengeForTwitchScheme()
    {
        var       harness = CreateHarness();
        using var db      = harness.Db;

        var result = harness.Controller.Login();

        var challenge = result as ChallengeResult;
        Assert.That(challenge,                        Is.Not.Null);
        Assert.That(challenge!.AuthenticationSchemes, Is.EquivalentTo(new[] { "Twitch" }));
    }
}
