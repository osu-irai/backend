using System.ComponentModel.DataAnnotations;

namespace irai.Features.Requests.Persistence;

/// <summary>
/// Database model for beatmapsets
/// </summary>
public class BeatmapSetModel
{
    [Key]
    public int Id { get; set; }

    public string Artist { get; set; } = null!;

    public string Title { get; set; } = null!;

    public int CreatorId { get; set; }
}
