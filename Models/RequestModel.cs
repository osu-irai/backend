using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace osuRequestor.Models;

public class RequestModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [DataType(DataType.DateTime)]
    public DateTime Date { get; set; }
    public required BeatmapModel Beatmap { get; set; }
    public required UserModel RequestedTo { get; set; }
    public required UserModel RequestedFrom { get; set; }

    public bool IsDeleted { get; set; } = false;
}