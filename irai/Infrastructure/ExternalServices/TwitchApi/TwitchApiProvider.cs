using System.Net;
using System.Net.Http.Headers;
using ChiffadicErrors;
using irai.Infrastructure.Configuration;
using irai.Infrastructure.ExternalServices.TwitchApi.Models;
using Microsoft.Extensions.Options;
using static ChiffadicErrors.Result;

namespace irai.Infrastructure.ExternalServices.TwitchApi;

public class TwitchApiProvider(
    IOptions<TwitchConfig> config,
    HttpClient httpClient,
    ILogger<TwitchApiProvider> logger)
{
    private const string OAUTH_ROOT = "https://id.twitch.tv/oauth2";
    private readonly TwitchConfig _config = config.Value;

    public async Task<Result<TwitchValidation, TwitchApiError>> ValidateUser(string token)
    {
        var requestMessage = new HttpRequestMessage
        {
            Method     = HttpMethod.Get,
            RequestUri = new Uri(OAUTH_ROOT + "/validate"),
            Headers    = { Authorization = new AuthenticationHeaderValue("Bearer", token) }
        };

        var response = await httpClient.SendAsync(requestMessage);

        // Invalid or expired token.
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            logger.LogWarning("Twitch token rejected with {StatusCode}", response.StatusCode);
            return Error<TwitchApiError>(new InvalidTwitchToken());
        }

        response.EnsureSuccessStatusCode();

        var validation = await response.Content.ReadFromJsonAsync<TwitchValidation>();
        return validation is null
                   ? Error<TwitchApiError>(new InvalidTwitchToken())
                   : Ok(validation);
    }

    public async Task<TwitchTokenResponse?> GetClientCredentials()
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id",     this._config.ClientId),
            new KeyValuePair<string, string>("client_secret", this._config.ClientSecret),
            new KeyValuePair<string, string>("grant_type",    "client_credentials")
        });

        var url      = new Uri(OAUTH_ROOT + "/token");
        var response = await httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TwitchTokenResponse>();
    }

    public async Task<Result<TwitchUserList, TwitchApiError>> GetUser(string userId, string token, string clientId)
    {
        var requestMessage = new HttpRequestMessage
        {
            Method     = HttpMethod.Get,
            RequestUri = new Uri("https://api.twitch.tv/helix/users?id=" + userId),
            Headers    = { Authorization = new AuthenticationHeaderValue("Bearer", token) }
        };
        requestMessage.Headers.Add("Client-Id", clientId);

        var response = await httpClient.SendAsync(requestMessage);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            logger.LogWarning("Twitch token rejected for user {UserId} with {StatusCode}", userId,
                              response.StatusCode);
            return Error<TwitchApiError>(new InvalidTwitchToken());
        }

        response.EnsureSuccessStatusCode();

        var users = await response.Content.ReadFromJsonAsync<TwitchUserList>();
        return Ok(users ?? new TwitchUserList());
    }
}
