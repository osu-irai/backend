using osuRequestor.Features.Search.Models;

namespace osuRequestor.Features.Search.Services;

public interface ISearchService
{
    Task<SearchUserResponse> SearchPlayers(int currentUser, string? query);
    Task<SearchBeatmapResponse> SearchBeatmaps(string? query);
}
