using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
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

    public Beatmap IntoApiModel()
    {
        return new Beatmap
        {
            Id = this.Id,
            BeatmapSet = new BeatmapSet
            {
                Id = this.BeatmapSet.Id,
                Artist = this.BeatmapSet.Artist,
                Title = this.BeatmapSet.Title,
                CreatorId = this.BeatmapSet.CreatorId,
            },
            Version = this.Version,
            ApproachRate = this.ApproachRate,
            OverallDifficulty = this.OverallDifficulty,
            CircleSize = this.CircleSize,
            HealthDrain = this.HealthDrain,
            BeatsPerMinute = this.BeatsPerMinute,
            Circles = this.Circles,
            Sliders = this.Sliders,
            Spinners = this.Spinners,
            StarRating = this.StarRating,
            Status = this.Status,
            MaxCombo = this.MaxCombo,
            Mode = this.Mode,
        };
    }

}