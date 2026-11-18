using irai.Common.Models;

namespace irai.Features.Requests.Models;

public record SentRequestResponse
{
    public required int Id { get; set; }

    public required BeatmapDTO Beatmap { get; set; }

    public required UserDTO To { get; set; }
}
