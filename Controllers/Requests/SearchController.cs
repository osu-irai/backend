using Microsoft.AspNetCore.Mvc;
using osu.NET;
using osuRequestor.Data;
using osuRequestor.DTO.Responses;
using osuRequestor.Exceptions;
using osuRequestor.Extensions;
using osuRequestor.Persistence;

namespace osuRequestor.Controllers.Requests;

[ApiController]
[Route("api/search")]
public class SearchController(ILogger<RequestController> logger, DatabaseContext dbContext, OsuApiClient osuClient)
    : ControllerBase
{
    private int _claim()
    {
        return HttpContext.User.Identity.ThrowIfUnauthorized().OrOnNullName();
    }

    /// <summary>
    ///     Search for players whose nickname starts with <see cref="query" />
    /// </summary>
    /// <param name="query">Username query</param>
    /// <returns>List of usernames <see cref="SearchUserResponse" /></returns>
    [HttpGet]
    [Route("player")]
    public async Task<ActionResult<SearchUserResponse>> GetPlayers(string? query)
    {
        logger.LogInformation("Queried players: {Query}", query);
        var users = await dbContext.QueryUsers(query);
        var response = new SearchUserResponse
        {
            Players = users.Select(u => u.IntoDTO()).ToList(),
            Count = users.Count
        };
        logger.LogInformation("Players: {Player}, count: {Count}", response.Players.FirstOrDefault(), response.Count);
        return Ok(response);
    }

    /// <summary>
    ///     Search for beatmaps. Uses osu!api as the source of beatmaps
    /// </summary>
    /// <param name="query">Query string</param>
    /// <returns>List of first 20 beatmaps found</returns>
    [HttpGet]
    [Route("beatmap")]
    public async Task<ActionResult<SearchBeatmapResponse>> GetBeatmaps(string? query)
    {
        var claim = _claim();

        var beatmaps = await osuClient.SearchBeatmapSetsAsync(query ?? string.Empty)
            .BadGatewayOnFailure("Beatmaps not found")
            .OrNotFound();
        var maps = beatmaps.ToBeatmapDtoList();
        var response = new SearchBeatmapResponse
        {
            Beatmaps = maps,
            Count = maps.Count
        };
        logger.LogInformation("Beatmaps: {FirstMap}, count: {Count}", response.Beatmaps.FirstOrDefault(),
            response.Count);
        return Ok(response);
    }
}