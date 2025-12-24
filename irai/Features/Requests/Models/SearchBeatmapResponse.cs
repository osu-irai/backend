using osuRequestor.Common.Models;

namespace osuRequestor.Features.Requests.Models;

public record SearchBeatmapResponse
{
    public required List<BeatmapDTO> Beatmaps { get; set; }
    public required int Count { get; set; }
}
