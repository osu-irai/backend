using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace osuRequestor.Models;

[PrimaryKey(nameof(BeatmapId))]
public class BeatmapModel
{
    public int BeatmapId { get; set; }
    public int BeatmapSetId { get; set; }
    [MaxLength(100)]
    public string Artist { get; set; }
    [MaxLength(100)]
    public string Title { get; set; }
    public double DifficultyRating { get; set; }

    public BeatmapModel(int beatmapId, int beatmapSetId, string artist, string title, double difficultyRating)
    {
        BeatmapId = beatmapId;
        BeatmapSetId = beatmapSetId;
        Artist = artist;
        Title = title;
        DifficultyRating = difficultyRating;
    }

}