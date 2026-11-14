using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using osuRequestor.Features.Users.Persistence;

namespace osuRequestor.Features.Requests.Persistence;

/// <summary>
/// Database model for beatmap requests
/// </summary>
public class RequestModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime Date { get; set; }

    public required BeatmapModel Beatmap { get; set; }
    public required UserModel RequestedTo { get; set; }
    public required UserModel? RequestedFrom { get; set; }

    public required RequestSource Source { get; set; }

    public bool IsDeleted { get; set; } = false;
}
