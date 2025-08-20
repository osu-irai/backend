using System.ComponentModel.DataAnnotations;
using osuRequestor.Apis.OsuApi.Models;

namespace osuRequestor.Models;

public class BeatmapSetModel
{
    [Key]
    public int Id { get; set; }

    public string Artist { get; set; } = null!;

    public string Title { get; set; } = null!;

    public int CreatorId { get; set; }

    public BeatmapSet IntoApiModel()
    {
        return new BeatmapSet
        {
            Id = this.Id,
            Artist = this.Artist,
            Title = this.Title,
            CreatorId = this.CreatorId,
        };
    }

    public static BeatmapSetModel FromBeatmapSet(osu.NET.Models.Beatmaps.BeatmapSet? set)
    {
        if (set is null)
        {
            return new BeatmapSetModel();
        }
        return new BeatmapSetModel
        {
            Id = set.Id,
            Artist = set.Artist,
            Title = set.Title,
            CreatorId = set.CreatorId
        };
    }
}