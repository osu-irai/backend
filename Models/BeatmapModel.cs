using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace osuRequestor.Models;

[PrimaryKey(nameof(BeatmapId))]
public class BeatmapModel
{
    public int BeatmapId { get; set; }
    public int BeatmapSetId { get; set; }
    [MaxLength(100)]
    public required string Artist { get; set; }
    [MaxLength(100)]
    public required string Title { get; set; }
    public double DifficultyRating { get; set; }

}