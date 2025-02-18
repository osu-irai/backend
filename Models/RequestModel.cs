namespace osuRequestor.Models;

public class RequestModel
{
    private BeatmapModel? BeatmapModel { get; set; }
    private int RequesteeId { get; set; }
    private int RequesteeName { get; set; }
    private RequestSource Source { get; set; }
}