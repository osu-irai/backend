using System.Text.Json.Serialization;

namespace osuRequestor.Infrastructure.ExternalServices.OsuApi.Models;

public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = null!;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}
