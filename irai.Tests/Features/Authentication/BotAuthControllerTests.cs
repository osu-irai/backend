using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using osuRequestor.Features.Authentication.Controllers;
using osuRequestor.Features.Authentication.Models;
using osuRequestor.Infrastructure.Configuration;

namespace osuRequestor.Tests.Features.Authentication;

[TestFixture]
public class BotAuthControllerTests
{
    private static BotAuthController CreateController()
    {
        var clients = new AuthClientsConfig
        {
            ["Irc"]    = new AuthClient { Name = "Irc", ClientSecret    = "irc-secret" },
            ["Twitch"] = new AuthClient { Name = "Twitch", ClientSecret = "twitch-secret" }
        };

        var config = new AuthConfig
        {
            SecretKey         = "this-is-a-test-signing-key-with-enough-bytes",
            Issuer            = "https://tests.local/",
            Audience          = "https://tests.local/api/bot",
            ExpirationMinutes = 60
        };

        return new BotAuthController(
            NullLogger<BotAuthController>.Instance,
            Options.Create(clients),
            Options.Create(config));
    }

    /// <summary>
    /// Login tokens should have expiration time
    /// </summary>
    [Test]
    public void Login_ReturnsTokenExpiringAfterConfiguredMinutes()
    {
        var controller = CreateController();

        var result = controller.Login(new TokenRequest
        {
            Name         = "Irc",
            ClientSecret = "irc-secret"
        });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(((OkObjectResult)result).Value as string);
        var expected = DateTime.Today.AddMinutes(60)
                               .ToUniversalTime();
        Assert.That(jwt.ValidTo, Is.EqualTo(expected)
                                   .Within(TimeSpan.FromSeconds(2)));
    }

    /// <summary>
    /// Logins with wrong data must not leak details, return HTTP 401
    /// </summary>
    [Test]
    public void Login_WithUnknownClient_ReturnsUnauthorized()
    {
        var controller = CreateController();

        var result = controller.Login(new TokenRequest
        {
            Name         = "fake",
            ClientSecret = "irc-secret"
        });

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    /// <summary>
    /// Happy path
    /// </summary>
    [Test]
    public void Login_WithValidCredentials_ReturnsJwt()
    {
        var controller = CreateController();

        var result = controller.Login(new TokenRequest
        {
            Name         = "Irc",
            ClientSecret = "irc-secret"
        });

        var ok    = result as OkObjectResult;
        var token = ok?.Value as string;
        var jwt   = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.That(ok,            Is.Not.Null);
        Assert.That(token,         Is.Not.Null.And.Not.Empty);
        Assert.That(jwt.Issuer,    Is.EqualTo("https://tests.local/"));
        Assert.That(jwt.Audiences, Is.EquivalentTo(["https://tests.local/api/bot/irc"]));
    }

    /// <summary>
    /// Happy path
    /// </summary>
    [Test]
    public void Login_WithValidTwitchClient_ReturnsTwitchAudience()
    {
        var controller = CreateController();

        var result = controller.Login(new TokenRequest
        {
            Name         = "Twitch",
            ClientSecret = "twitch-secret"
        });

        var ok  = result as OkObjectResult;
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(ok?.Value as string);
        Assert.That(ok,            Is.Not.Null);
        Assert.That(jwt.Audiences, Is.EquivalentTo(["https://tests.local/api/bot/twitch"]));
    }

    /// <summary>
    /// Logins with wrong data must not leak details, return HTTP 401
    /// </summary>
    [Test]
    public void Login_WithWrongClientSecret_ReturnsUnauthorized()
    {
        var controller = CreateController();

        var result = controller.Login(new TokenRequest
        {
            Name         = "Irc",
            ClientSecret = "wrong"
        });

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }
}
