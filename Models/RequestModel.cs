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
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime Date { get; set; }
    public BeatmapModel Beatmap { get; set; }
    public UserModel RequestedTo { get; set; }
    public UserModel RequestedFrom { get; set; }

    public RequestModel() { }

    public RequestModel(BeatmapModel beatmap, UserModel requestedFrom, UserModel requestedTo)
    {
        Beatmap = beatmap;
        RequestedFrom = requestedFrom;
        RequestedTo = requestedTo;
    }
}