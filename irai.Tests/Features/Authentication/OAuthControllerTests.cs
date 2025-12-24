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
using osuRequestor.Infrastructure.ExternalServices.OsuApi.Interfaces;
using osuRequestor.Infrastructure.ExternalServices.OsuApi.Models;
using osuRequestor.Infrastructure.Persistence;
using osuRequestor.Infrastructure.Persistence.Models;

namespace osuRequestor.Tests.Features.Authentication;

[TestFixture]
public class OAuthControllerTests
{
    private static void SetupExternalAuth(IAuthenticationService authentication,
                                          string? accessToken = "access-token", string? refreshToken = "refresh-token")
    {
        var properties = new AuthenticationProperties();
        var tokens     = new List<AuthenticationToken>();
        if (accessToken is not null)
        {
            tokens.Add(new AuthenticationToken { Name = "access_token", Value = accessToken });
        }

        if (refreshToken is not null)
        {
            tokens.Add(new AuthenticationToken { Name = "refresh_token", Value = refreshToken });
        }

        properties.StoreTokens(tokens);

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "external")], "ExternalCookies");
        var ticket   = new AuthenticationTicket(new ClaimsPrincipal(identity), properties, "ExternalCookies");
        authentication
            .AuthenticateAsync(Arg.Any<HttpContext>(), "ExternalCookies")
            .Returns(AuthenticateResult.Success(ticket));
    }

    private static Harness CreateHarness()
    {
        var db             = CreateDatabase();
        var osuApi         = Substitute.For<IOsuApiProvider>();
        var authentication = Substitute.For<IAuthenticationService>();
        authentication.SignInAsync(Arg.Any<HttpContext>(), Arg.Any<string>(),
                                   Arg.Any<ClaimsPrincipal>(), Arg.Any<AuthenticationProperties>())
                      .Returns(Task.CompletedTask);
        authentication.SignOutAsync(Arg.Any<HttpContext>(), Arg.Any<string>(),
                                    Arg.Any<AuthenticationProperties>())
                      .Returns(Task.CompletedTask);

        var controller = new OAuthController(
            NullLogger<OAuthController>.Instance,
            osuApi,
            new global::osuRequestor.Features.Authentication.Services.AuthenticationService(
                db, NullLogger<global::osuRequestor.Features.Authentication.Services.AuthenticationService>.Instance),
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
                ControllerName = "OAuth",
                ActionName     = "Authenticate"
            }
        };

        return new Harness(db, controller, osuApi, authentication);
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
        OAuthController Controller,
        IOsuApiProvider OsuApi,
        IAuthenticationService Authentication);

    /// <summary>
    /// Happy path
    /// </summary>
    [Test]
    public void Authenticate_ReturnsChallengeForOsuScheme()
    {
        var       harness = CreateHarness();
        using var db      = harness.Db;

        var result = harness.Controller.Authenticate();

        var challenge = result as ChallengeResult;
        Assert.That(challenge,                        Is.Not.Null);
        Assert.That(challenge!.AuthenticationSchemes, Is.EquivalentTo(["osu"]));
    }

    /// <summary>
    /// Mock authentication failure
    /// </summary>
    [Test]
    public async Task CompleteAuthentication_WhenExternalAuthFails_ReturnsForbid()
    {
        var             harness = CreateHarness();
        await using var db      = harness.Db;
        harness.Authentication
               .AuthenticateAsync(Arg.Any<HttpContext>(), "ExternalCookies")
               .Returns(AuthenticateResult.NoResult());

        var result = await harness.Controller.CompleteAuthentication();

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task CompleteAuthentication_WhenProviderThrows_ReturnsForbid()
    {
        var             harness = CreateHarness();
        await using var db      = harness.Db;
        SetupExternalAuth(harness.Authentication);
        harness.OsuApi.GetSelfUser("access-token")
               .Returns(Task.FromException<User?>(new HttpRequestException("osu down")));

        var result = await harness.Controller.CompleteAuthentication();

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task CompleteAuthentication_WhenRefreshTokenIsMissing_ReturnsForbid()
    {
        var             harness = CreateHarness();
        await using var db      = harness.Db;
        SetupExternalAuth(harness.Authentication, refreshToken: null);
        harness.OsuApi.GetSelfUser("access-token")
               .Returns(new User
               {
                   Id          = 7,
                   Username    = "alice",
                   CountryCode = "US",
                   AvatarUrl   = "https://a.ppy.sh/7"
               });

        var result = await harness.Controller.CompleteAuthentication();

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    /// <summary>
    /// User relogin
    /// </summary>
    [Test]
    public async Task CompleteAuthentication_WhenUserExists_UpdatesUsernameAndToken()
    {
        var             harness = CreateHarness();
        await using var db      = harness.Db;
        db.Users.Add(new UserModel
        {
            Id          = 7,
            Username    = "old-name",
            CountryCode = "GB",
            AvatarUrl   = "https://a.ppy.sh/7",
            Settings    = new SettingsModel()
        });
        db.Tokens.Add(new TokenModel
        {
            UserId       = 7,
            AccessToken  = "old-access",
            RefreshToken = "old-refresh",
            Expires      = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        SetupExternalAuth(harness.Authentication);
        harness.OsuApi.GetSelfUser("access-token")
               .Returns(new User
               {
                   Id          = 7,
                   Username    = "new-name",
                   CountryCode = "US",
                   AvatarUrl   = "https://a.ppy.sh/7"
               });

        var result = await harness.Controller.CompleteAuthentication();

        var user  = await db.Users.SingleAsync();
        var token = await db.Tokens.SingleAsync();
        Assert.That(result,             Is.InstanceOf<RedirectResult>());
        Assert.That(user.Username,      Is.EqualTo("new-name"));
        Assert.That(token.AccessToken,  Is.EqualTo("access-token"));
        Assert.That(token.RefreshToken, Is.EqualTo("refresh-token"));
    }

    /// <summary>
    /// User registration
    /// </summary>
    [Test]
    public async Task CompleteAuthentication_WhenUserIsNew_CreatesUserAndTokenAndRedirects()
    {
        var             harness = CreateHarness();
        await using var db      = harness.Db;
        SetupExternalAuth(harness.Authentication);
        harness.OsuApi.GetSelfUser("access-token")
               .Returns(new User
               {
                   Id          = 7,
                   Username    = "alice",
                   CountryCode = "US",
                   AvatarUrl   = "https://a.ppy.sh/7"
               });

        var result = await harness.Controller.CompleteAuthentication();

        var user = await db.Users.Include(userModel => userModel.Settings)
                           .SingleAsync();
        var token = await db.Tokens.SingleAsync();
        Assert.That(result,                       Is.InstanceOf<RedirectResult>());
        Assert.That(((RedirectResult)result).Url, Is.EqualTo("/home"));
        Assert.That(user.Id,                      Is.EqualTo(7));
        Assert.That(user.Username,                Is.EqualTo("alice"));
        Assert.That(user.Settings,                Is.Not.Null);
        Assert.That(token.UserId,                 Is.EqualTo(7));
        Assert.That(token.AccessToken,            Is.EqualTo("access-token"));
        Assert.That(token.RefreshToken,           Is.EqualTo("refresh-token"));
        await harness.Authentication.Received(1)
                     .SignInAsync(
                         Arg.Any<HttpContext>(), "InternalCookies", Arg.Any<ClaimsPrincipal>(),
                         Arg.Any<AuthenticationProperties>());
    }
}
