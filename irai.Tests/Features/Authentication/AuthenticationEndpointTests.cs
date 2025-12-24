using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using osuRequestor.Tests.Infrastructure.Harness;

namespace osuRequestor.Tests.Features.Authentication;

[TestFixture]
[Category("Integration")]
public class AuthenticationEndpointTests : IntegrationTestBase
{
    [Test]
    public async Task BotLogin_WithValidCredentials_ReturnsJwt()
    {
        var response = await this.Client.PostAsJsonAsync("/api/bot/auth", new
        {
            name         = "Irc",
            clientSecret = "irc-test-secret"
        });

        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadAsStringAsync();
        var jwt   = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.That(token,         Is.Not.Null.And.Not.Empty);
        Assert.That(jwt.Issuer,    Is.EqualTo("https://tests.local/"));
        Assert.That(jwt.Audiences, Has.Member("http://localhost:5076/api/bot/irc"));
    }

    [Test]
    public async Task BotLogin_WithWrongSecret_ReturnsUnauthorized()
    {
        var response = await this.Client.PostAsJsonAsync("/api/bot/auth", new
        {
            name         = "Irc",
            clientSecret = "wrong"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task OsuChallenge_RedirectsToOsuAuthorization()
    {
        using var client = this.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/oauth/auth");

        Assert.That(response.StatusCode,       Is.EqualTo(HttpStatusCode.Redirect));
        Assert.That(response.Headers.Location, Is.Not.Null);
        Assert.That(
            response.Headers.Location!.ToString(),
            Does.StartWith("https://osu.ppy.sh/oauth/authorize"));
    }
}
