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

    public static BeatmapModel FromBeatmapExtended(BeatmapExtended beatmap)
    {
        return new BeatmapModel
        {
            Id = beatmap.Id,
            BeatmapSet = BeatmapSetModel.FromBeatmapSet(beatmap.Set),
            Version = beatmap.Version,
            ApproachRate = beatmap.ApproachRate,
            OverallDifficulty = beatmap.OverallDifficulty,
            CircleSize = beatmap.CircleSize,
            HealthDrain = beatmap.HealthDrain,
            BeatsPerMinute = beatmap.BPM,
            Circles = beatmap.CountCircles,
            Sliders = beatmap.CountSliders,
            Spinners = beatmap.CountSpinners,
            StarRating = beatmap.DifficultyRating,
            Status = beatmap.Status.IntoBeatmapStatus(),
            MaxCombo = beatmap.MaxCombo ?? 0,
            Mode = beatmap.Ruleset.IntoMode() 
        };
    }

}