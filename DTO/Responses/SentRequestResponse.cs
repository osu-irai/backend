using osuRequestor.DTO.General;

namespace osuRequestor.DTO.Responses;

public class SentRequestResponse
{
    public required int Id { get; set; }

    public required BeatmapDTO Beatmap { get; set; }
    
    public required UserDTO To { get; set; }
}