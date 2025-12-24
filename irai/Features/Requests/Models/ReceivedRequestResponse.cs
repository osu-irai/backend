using osuRequestor.Common.Models;
using osuRequestor.Infrastructure.Persistence.Models;

namespace osuRequestor.Features.Requests.Models;

public class ReceivedRequestResponse
{
    public required int Id { get; set; }

    public required BeatmapDTO Beatmap { get; set; }

    public UserDTO? From { get; set; }
    public required RequestSource Source { get; set; }
}
