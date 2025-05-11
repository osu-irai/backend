namespace osuRequestor.DTO.General;

public class BeatmapDTO
{
    public required int BeatmapId { get; set; }
    
    public required int BeatmapsetId { get; set; }
    
    public required string Artist { get; set; }
    
    public required string Title { get; set; }
    
    // Called Version in BeatmapModel
    public required string Difficulty { get; set; }
    
    public required double Stars { get; set; }
}