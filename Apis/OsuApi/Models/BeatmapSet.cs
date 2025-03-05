using System.Text.Json.Serialization;
using osuRequestor.Models;

namespace osuRequestor.Apis.OsuApi.Models
{
    public class BeatmapSet
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("artist")]
        public string Artist { get; set; } = null!;

        [JsonPropertyName("title")]
        public string Title { get; set; } = null!;

        [JsonPropertyName("user_id")]
        public int CreatorId { get; set; }

        public BeatmapSetModel IntoModel()
        {
            return new BeatmapSetModel
            {
                Id = this.Id,
                Artist = this.Artist,
                Title = this.Title,
                CreatorId = this.CreatorId,
            };
        }
    }
}
