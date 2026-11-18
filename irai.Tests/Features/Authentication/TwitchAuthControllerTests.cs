using System.Security.Claims;
using Hangfire;
using irai.Features.Authentication.Controllers;
using irai.Features.Authentication.Services;
using irai.Infrastructure.Configuration;
using irai.Infrastructure.ExternalServices.TwitchApi;
using irai.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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
using IAuthenticationService = Microsoft.AspNetCore.Authentication.IAuthenticationService;
using Services_AuthenticationService = irai.Features.Authentication.Services.AuthenticationService;

namespace irai.Tests.Features.Authentication;

[TestFixture]
public class TwitchAuthControllerTests
{
    private IBackgroundJobClient _backgroundJobClient = null!;
    private IFriendService _friendService = null!;

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

    private Harness CreateHarness()
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

        this._backgroundJobClient = Substitute.For<IBackgroundJobClient>();
        this._friendService       = Substitute.For<IFriendService>();
        var controller = new TwitchAuthController(
            NullLogger<TwitchAuthController>.Instance,
            new Services_AuthenticationService(this._backgroundJobClient, this._friendService, db,
                                               DataProtectionProvider.Create("irai-tests"),
                                               NullLogger<Services_AuthenticationService>.Instance),
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

        Assert.That(result,        Is.Not.Null);
        Assert.That(result.Result, Is.InstanceOf<UnauthorizedHttpResult>());
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

        Assert.That(result.Result, Is.InstanceOf<UnauthorizedHttpResult>());
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
        Assert.That(result,                       Is.Not.Null);
        Assert.That(result.AuthenticationSchemes, Is.EquivalentTo(new[] { "Twitch" }));
    }
}
