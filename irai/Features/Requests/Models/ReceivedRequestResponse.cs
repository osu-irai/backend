using irai.Common.Models;
using irai.Features.Requests.Persistence;

namespace irai.Features.Requests.Models;

public class ReceivedRequestResponse
{
    public required int Id { get; set; }

    public required BeatmapDTO Beatmap { get; set; }

    public UserDTO? From { get; set; }
    public required RequestSource Source { get; set; }
}
