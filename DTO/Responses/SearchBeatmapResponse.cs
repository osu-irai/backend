using osuRequestor.DTO.General;

namespace osuRequestor.DTO.Responses;

public record SearchBeatmapResponse
{
    public required List<BeatmapDTO> Beatmaps { get; set; }
    public required int Count { get; set; }
}