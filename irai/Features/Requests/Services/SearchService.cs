using Microsoft.EntityFrameworkCore;
using osu.NET;
using osuRequestor.Common.Extensions;
using osuRequestor.Features.Requests.Mappings;
using osuRequestor.Features.Requests.Models;
using osuRequestor.Infrastructure.Persistence;

namespace osuRequestor.Features.Requests.Services;

public class SearchService(
    DatabaseContext dbContext,
    OsuApiClient osuClient,
    ILogger<SearchService> logger) : ISearchService
{
    public async Task<SearchUserResponse> SearchPlayers(string? query)
    {
        logger.LogInformation("Queried players: {Query}", query);
        var queryString = query ?? "";
        var users = await dbContext.Users
                                   .AsNoTracking()
                                   .Where(u => u.Username.ToLower()
                                                .StartsWith(queryString.ToLower()))
                                   .OrderBy(u => u.Username)
                                   .Take(10)
                                   .ToListAsync();
        var response = new SearchUserResponse
        {
            Players = users.Select(u => u.IntoDTO())
                           .ToList(),
            Count = users.Count
        };
        logger.LogInformation("Players: {Player}, count: {Count}", response.Players.FirstOrDefault(), response.Count);
        return response;
    }

    public async Task<SearchBeatmapResponse> SearchBeatmaps(string? query)
    {
        var beatmaps = await osuClient.SearchBeatmapSetsAsync(query ?? "")
                                      .BadGatewayOnFailure("Beatmaps not found")
                                      .OrNotFound();
        var maps = beatmaps.ToBeatmapDtoList();
        var response = new SearchBeatmapResponse
        {
            Beatmaps = maps,
            Count    = maps.Count
        };
        logger.LogInformation("Beatmaps: {FirstMap}, count: {Count}", response.Beatmaps.FirstOrDefault(),
                              response.Count);
        return response;
    }
}
