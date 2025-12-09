using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using osuRequestor.Apis.TwitchApi.Models;
using osuRequestor.Configuration;

namespace osuRequestor.Apis.TwitchApi;

public class TwitchApiProvider(IOptions<TwitchConfig> config, HttpClient httpClient, ILogger<TwitchApiProvider> logger)
{
    private const string OauthRoot = "https://id.twitch.tv/oauth2";
    private readonly TwitchConfig _config = config.Value;
    private readonly ILogger<TwitchApiProvider> _logger = logger;

    public async Task<TwitchValidation?> ValidateUser(string token)
    {
        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(OauthRoot + "/validate"),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) }
        };

        var response = await httpClient.SendAsync(requestMessage);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TwitchValidation>();
    }

    public async Task<TwitchTokenResponse?> GetClientCredentials()
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _config.ClientId),
            new KeyValuePair<string, string>("client_secret", _config.ClientSecret),
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        var url = new Uri(OauthRoot + "/token");
        var response = await httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TwitchTokenResponse>();
    }

    public async Task<TwitchUserList> GetUser(string userId, string token, string clientId)
    {
        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("https://api.twitch.tv/helix/users?id=" + userId),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) }
        };
        requestMessage.Headers.Add("Client-Id", clientId);

        var response = await httpClient.SendAsync(requestMessage);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TwitchUserList>() ?? new TwitchUserList();
    }
}