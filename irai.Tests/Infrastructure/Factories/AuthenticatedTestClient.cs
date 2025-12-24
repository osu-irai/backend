using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using osuRequestor.Tests.Infrastructure.Harness;

namespace osuRequestor.Tests.Infrastructure.Factories;

public static class AuthenticatedTestClient
{
    // Must match the values configured by CustomWebApplicationFactory.
    public const string SigningKey = "test-signing-key-with-at-least-thirty-two-bytes";
    public const string Issuer = "https://tests.local/";
    public const string Audience = "http://localhost:5076/api/bot";

    public static async Task<HttpClient> CreateIrcClientAsync(
        CustomWebApplicationFactory factory,
        string name = "Irc",
        string clientSecret = "irc-test-secret")
    {
        var client   = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/bot/auth", new { name, clientSecret });
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException("Bot authentication returned an empty token.");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static Task<HttpClient> CreateTwitchClientAsync(CustomWebApplicationFactory factory)
    {
        return CreateIrcClientAsync(factory, "Twitch", "twitch-test-secret");
    }

    public static string CreateJwt(
        int userId,
        string? role = "User",
        string? audience = null,
        string? signingKey = null,
        string? issuer = null)
    {
        var key                = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey ?? SigningKey));
        var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim> { new Claim(ClaimTypes.Name, userId.ToString()) };
        if (role is not null)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer   ?? Issuer,
            audience ?? $"{Audience}/user",
            claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static HttpClient CreateClientWithToken(CustomWebApplicationFactory factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static HttpClient CreateJwtClient(
        CustomWebApplicationFactory factory,
        int userId,
        string? role = "User",
        string? audience = null)
    {
        return CreateClientWithToken(factory, CreateJwt(userId, role, audience));
    }

    /// <summary>
    /// Builds a valid InternalCookies authentication cookie using the application's
    /// own data protection keys and cookie options, so the real cookie handler accepts it.
    /// </summary>
    public static HttpClient CreateInternalCookiesClient(
        CustomWebApplicationFactory factory,
        int userId,
        string role = "User")
    {
        var options = factory.Services
                             .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
                             .Get("InternalCookies");

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            ],
            "InternalCookies");

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), "InternalCookies");
        var cookie = options.TicketDataFormat.Protect(ticket);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", $"{options.Cookie.Name}={cookie}");
        return client;
    }
}
