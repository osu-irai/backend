using System.Text.Json.Serialization;
using osuRequestor.Infrastructure.Persistence.Models;

namespace osuRequestor.Infrastructure.ExternalServices.OsuApi.Models;

public class User
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = null!;

    [JsonPropertyName("country_code")]
    public string CountryCode { get; set; } = null!;

    [JsonPropertyName("avatar_url")]
    public string AvatarUrl { get; set; } = null!;

    public UserModel IntoModel()
    {
        return new UserModel
        {
            Id        = this.Id,
            Username  = this.Username,
            AvatarUrl = this.AvatarUrl,
            Settings  = new SettingsModel()
        };
    }
}
