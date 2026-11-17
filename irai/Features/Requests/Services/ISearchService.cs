using osuRequestor.Features.Requests.Models;

namespace osuRequestor.Features.Requests.Services;

public interface ISearchService
{
    Task<SearchUserResponse> SearchPlayers(int currentUser, string? query);
    Task<SearchBeatmapResponse> SearchBeatmaps(string? query);
}
