using osuRequestor.DTO.General;

namespace osuRequestor.DTO.Responses;


public class ReceivedRequestResponse
{
    public required int Id { get; set; }

    public required BeatmapDTO Beatmap { get; set; }
    
    public required UserDTO From { get; set; }
}