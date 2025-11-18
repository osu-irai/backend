using System.Text.Json.Serialization;
using osuRequestor.Models;

namespace osuRequestor.Apis.OsuApi.Models
{
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
                Id = Id,
                Username = Username,
                AvatarUrl = AvatarUrl,
                Settings = new(),
            };
        }
    }
}
