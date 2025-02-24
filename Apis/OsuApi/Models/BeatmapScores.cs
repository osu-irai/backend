using System.Text.Json.Serialization;

namespace osuRequestor.Apis.OsuApi.Models
{
    public class BeatmapScores
    {
        [JsonPropertyName("scores")]
        public Score[] Scores { get; set; } = null!;
    }
}
