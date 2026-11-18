using System.IdentityModel.Tokens.Jwt;
using System.Net;
using irai.Tests.Infrastructure.Harness;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace irai.Tests.Features.Authentication;

[TestFixture]
[Category("Integration")]
public class AuthenticationIntegrationTests : IntegrationTestBase
{
    private HttpClient CreateNoRedirectClient()
    {
        return this.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    /// <summary>
    /// Happy path
    /// </summary>
    [Test]
    public async Task BotLogin_WithTwitchCredentials_ReturnsRawJwtForTwitchAudience()
    {
        var response = await this.Client.PostAsJsonAsync("/bot/auth", new
        {
            name         = "Twitch",
            clientSecret = "twitch-test-secret"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var token = await response.Content.ReadAsStringAsync();
        var jwt   = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.That(token,         Does.Not.StartWith("\""));
        Assert.That(jwt.Issuer,    Is.EqualTo("https://tests.local/"));
        Assert.That(jwt.Audiences, Has.Member("http://localhost:5076/bot/twitch"));
    }

    /// <summary>
    /// Login failure returns HTTP 401 without unrelated data
    /// </summary>
    [Test]
    public async Task BotLogin_WithUnknownClientName_ReturnsUnauthorized()
    {
        var response = await this.Client.PostAsJsonAsync("/bot/auth", new
        {
            name         = "DoesNotExist",
            clientSecret = "whatever"
        });
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(body,                Does.Not.Contain("KeyNotFoundException"));
        Assert.That(body,                Does.Not.Contain("   at "));
    }

    [Test]
    public async Task BotLogin_WithValidCredentials_ReturnsJwt()
    {
        var response = await this.Client.PostAsJsonAsync("/bot/auth", new
        {
            name         = "Irc",
            clientSecret = "irc-test-secret"
        });

        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadAsStringAsync();
        var jwt   = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.That(token,         Is.Not.Null.And.Not.Empty);
        Assert.That(jwt.Issuer,    Is.EqualTo("https://tests.local/"));
        Assert.That(jwt.Audiences, Has.Member("http://localhost:5076/bot/irc"));
    }

    [Test]
    public async Task BotLogin_WithWrongSecret_ReturnsUnauthorized()
    {
        var response = await this.Client.PostAsJsonAsync("/bot/auth", new
        {
            name         = "Irc",
            clientSecret = "wrong"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task OsuChallenge_RedirectsToOsuAuthorization()
    {
        using var client = CreateNoRedirectClient();

        var response = await client.GetAsync("/oauth/auth");

        Assert.That(response.StatusCode,       Is.EqualTo(HttpStatusCode.Redirect));
        Assert.That(response.Headers.Location, Is.Not.Null);
        Assert.That(
            response.Headers.Location!.ToString(),
            Does.StartWith("https://osu.ppy.sh/oauth/authorize"));
    }

    /// <summary>
    /// External auth failure
    /// </summary>
    [Test]
    public async Task OsuComplete_WithoutExternalCookie_ReturnsForbidden()
    {
        using var client = CreateNoRedirectClient();

        var response = await client.GetAsync("/oauth/complete");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    /// <summary>
    /// Happy path
    /// </summary>
    [Test]
    public async Task TwitchAuth_RedirectsToTwitchAuthorizationEndpoint()
    {
        using var client = CreateNoRedirectClient();

        var response = await client.GetAsync("/twitch/auth");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        Assert.That(
            response.Headers.Location!.ToString(),
            Does.StartWith("https://id.twitch.tv/oauth2/authorize"));
    }
}
