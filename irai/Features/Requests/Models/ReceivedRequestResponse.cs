using osuRequestor.Common.Models;
using osuRequestor.Features.Requests.Persistence;

namespace osuRequestor.Features.Requests.Models;

public class ReceivedRequestResponse
{
    public required int Id { get; set; }

    public required BeatmapDTO Beatmap { get; set; }

    public UserDTO? From { get; set; }
    public required RequestSource Source { get; set; }
}
