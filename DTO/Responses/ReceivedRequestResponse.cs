using osuRequestor.DTO.General;
using osuRequestor.Models;

namespace osuRequestor.DTO.Responses;


public record ReceivedRequestResponse
{
    public required int Id { get; set; }

    public required BeatmapDTO Beatmap { get; set; }
    
    public UserDTO? From { get; set; }
    public required RequestSource Source { get; set; }
}