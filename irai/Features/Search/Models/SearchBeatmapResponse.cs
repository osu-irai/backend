using irai.Common.Models;

namespace irai.Features.Search.Models;

public record SearchBeatmapResponse
{
    public required List<BeatmapDTO> Beatmaps { get; set; }
    public required int Count { get; set; }
}
