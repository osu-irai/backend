using System.ComponentModel.DataAnnotations;
using osuRequestor.Infrastructure.ExternalServices.OsuApi.Models;

namespace osuRequestor.Infrastructure.Persistence.Models;

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

    public BeatmapSet IntoApiModel()
    {
        return new BeatmapSet
        {
            Id        = this.Id,
            Artist    = this.Artist,
            Title     = this.Title,
            CreatorId = this.CreatorId
        };
    }
}
