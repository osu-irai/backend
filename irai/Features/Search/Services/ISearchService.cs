using irai.Features.Search.Models;

namespace irai.Features.Search.Services;

public interface ISearchService
{
    Task<SearchUserResponse> SearchPlayers(int currentUser, string? query);
    Task<SearchBeatmapResponse> SearchBeatmaps(string? query);
}
