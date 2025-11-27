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
}