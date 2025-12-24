using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using osuRequestor.Tests.Infrastructure.Harness;

namespace osuRequestor.Tests.Features.Authentication;

[TestFixture]
[Category("Integration")]
public class AuthenticationPipelineTests : IntegrationTestBase
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
        var response = await this.Client.PostAsJsonAsync("/api/bot/auth", new
        {
            name         = "Twitch",
            clientSecret = "twitch-test-secret"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var token = await response.Content.ReadAsStringAsync();
        var jwt   = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.That(token,         Does.Not.StartWith("\""));
        Assert.That(jwt.Issuer,    Is.EqualTo("https://tests.local/"));
        Assert.That(jwt.Audiences, Has.Member("http://localhost:5076/api/bot/twitch"));
    }

    /// <summary>
    /// Login failure returns HTTP 401 without unrelated data
    /// </summary>
    [Test]
    public async Task BotLogin_WithUnknownClientName_ReturnsUnauthorized()
    {
        var response = await this.Client.PostAsJsonAsync("/api/bot/auth", new
        {
            name         = "DoesNotExist",
            clientSecret = "whatever"
        });
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(body,                Does.Not.Contain("KeyNotFoundException"));
        Assert.That(body,                Does.Not.Contain("   at "));
    }

    /// <summary>
    /// External auth failure
    /// </summary>
    [Test]
    public async Task OsuComplete_WithoutExternalCookie_RedirectsToAccessDenied()
    {
        using var client = CreateNoRedirectClient();

        var response = await client.GetAsync("/api/oauth/complete");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    /// <summary>
    /// Happy path
    /// </summary>
    [Test]
    public async Task TwitchAuth_RedirectsToTwitchAuthorizationEndpoint()
    {
        using var client = CreateNoRedirectClient();

        var response = await client.GetAsync("/api/twitch/auth");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        Assert.That(
            response.Headers.Location!.ToString(),
            Does.StartWith("https://id.twitch.tv/oauth2/authorize"));
    }
}
