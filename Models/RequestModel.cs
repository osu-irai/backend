using System.ComponentModel.DataAnnotations;

namespace osuRequestor.Models;

public class RequestModel
{
    public int Id { get; set; }
    [DataType(DataType.Date)]
    public DateTime Date { get; set; }
    private BeatmapModel? BeatmapModel { get; set; }
    private int RequestedToId { get; set; }
    
    [StringLength(20, MinimumLength = 3)]
    private int RequestedToName { get; set; }
    private RequestSource Source { get; set; }
}