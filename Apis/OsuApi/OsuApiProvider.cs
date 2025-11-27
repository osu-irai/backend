using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using osuRequestor.Apis.OsuApi.Interfaces;
using osuRequestor.Apis.OsuApi.Models;
using osuRequestor.Configuration;

namespace osuRequestor.Apis.OsuApi;

/// <summary>
///     Stolen entirely off of Relaxation Vault's wrapper until a real one exists
/// </summary>
public class OsuApiProvider : IOsuApiProvider
{
    private const string OsuBase = "https://osu.ppy.sh/";
    private const string ApiTokenLink = "oauth/token";
    private const string ApiMeLink = "api/v2/me/";
    private const string ApiFriendsLink = "api/v2/friends/";
    private readonly OsuApiConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OsuApiProvider> _logger;

    private TokenResponse? _userlessToken;
    private DateTime? _userlessTokenExpiration;

    public OsuApiProvider(IOptions<OsuApiConfig> config, HttpClient httpClient, ILogger<OsuApiProvider> logger)
    {
        _config = config.Value;
        _httpClient = httpClient;
        _logger = logger;

        RefreshUserlessToken().Wait();
    }

    public async Task<User?> GetSelfUser(string token)
    {
        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(OsuBase + ApiMeLink),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) }
        };

        var response = await _httpClient.SendAsync(requestMessage);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<User>();
    }

    public async Task<User[]?> GetFriends(string token)
    {
        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(OsuBase + ApiFriendsLink),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) }
        };

        var response = await _httpClient.SendAsync(requestMessage);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<User[]>();
    }

    public async Task<TokenResponse?> RefreshToken(string refreshToken, string accessToken)
    {
        var config = new RefreshTokenRequest
        {
            ClientId = _config.ClientId,
            ClientSecret = _config.ClientSecret,
            RefreshToken = refreshToken,
            AccessToken = accessToken
        };

        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(OsuBase + ApiTokenLink),
            Content = new StringContent(JsonSerializer.Serialize(config), null, "application/json"),
            Headers = { Accept = { new MediaTypeWithQualityHeaderValue("application/json") } }
        };

        var response = await _httpClient.SendAsync(requestMessage);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<TokenResponse>();
    }

    private async Task RefreshUserlessToken()
    {
        if (_userlessTokenExpiration > DateTime.UtcNow) return;

        var requestModel = new GetUserlessTokenRequest
        {
            ClientId = _config.ClientId,
            ClientSecret = _config.ClientSecret
        };

        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(OsuBase + ApiTokenLink),
            Content = new StringContent(JsonSerializer.Serialize(requestModel), null, "application/json"),
            Headers = { Accept = { new MediaTypeWithQualityHeaderValue("application/json") } }
        };

        var response = await _httpClient.SendAsync(requestMessage);

        if (!response.IsSuccessStatusCode)
        {
            _logger.Log(LogLevel.Error, "Couldn't update userless token! Status code: {Code}", response.StatusCode);
            return;
        }

        _userlessToken = await response.Content.ReadFromJsonAsync<TokenResponse>();
        if (_userlessToken == null)
        {
            _logger.Log(LogLevel.Error, "Couldn't parse userless token! {Json}",
                await response.Content.ReadAsStringAsync());
            return;
        }

        _userlessTokenExpiration = DateTime.UtcNow.AddSeconds(_userlessToken.ExpiresIn);
    }
}