using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using osu.NET.Models.Beatmaps;
using osuRequestor.Apis.OsuApi.Models;

namespace osuRequestor.Models;

[PrimaryKey(nameof(Id))]
public class BeatmapModel
{
    [Key]
    public int Id { get; set; }

    public BeatmapSetModel BeatmapSet { get; set; } = null!;

    public string Version { get; set; } = null!;

    public double ApproachRate { get; set; }

    public double OverallDifficulty { get; set; }

    public double CircleSize { get; set; }

    public double HealthDrain { get; set; }

    public double BeatsPerMinute { get; set; }

    public int Circles { get; set; }

    public int Sliders { get; set; }

    public int Spinners { get; set; }

    public double StarRating { get; set; }

    public BeatmapStatus Status { get; set; }

    public int MaxCombo { get; set; }

    public Mode Mode { get; set; }

}