using System.Text.Json;

namespace osuRequestor.DTO;


public class RequestResponse
{
    public required int Id { get; set; }

    public required BeatmapDTO Beatmap { get; set; }
    
    public required UserDTO From { get; set; }
}