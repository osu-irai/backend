using osuRequestor.DTO.General;

namespace osuRequestor.DTO.Responses;


public record ReceivedRequestResponse
{
    public required int Id { get; set; }

    public required BeatmapDTO Beatmap { get; set; }
    
    public required UserDTO From { get; set; }
}