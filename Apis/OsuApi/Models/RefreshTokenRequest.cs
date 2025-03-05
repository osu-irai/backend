using System.Text.Json.Serialization;

namespace osuRequestor.Apis.OsuApi.Models;

public class RefreshTokenRequest
{
    [JsonPropertyName("client_id")]
    public int ClientId { get; set; }

    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; set; } = null!;

    [JsonPropertyName("grant_type")]
    public string GrantType { get; set; } = "refresh_token";

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = null!;

    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = null!;
}