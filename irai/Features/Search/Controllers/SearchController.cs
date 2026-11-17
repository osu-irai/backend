using Microsoft.AspNetCore.Mvc;
using osuRequestor.Common.Controllers;
using osuRequestor.Features.Search.Models;
using osuRequestor.Features.Search.Services;

namespace osuRequestor.Features.Search.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController(
    ISearchService searchService)
    : CrudController
{
    /// <summary>
    /// Search for players whose nickname starts with <see cref="query" />
    /// </summary>
    /// <param name="query">Username query</param>
    /// <returns>List of usernames <see cref="SearchUserResponse" /></returns>
    [HttpGet]
    [Route("player")]
    public async Task<ActionResult<SearchUserResponse>> GetPlayers(string? query)
    {
        var id = await GetOsuClaim();
        return Ok(await searchService.SearchPlayers(id, query));
    }

    /// <summary>
    /// Search for beatmaps. Uses osu!api as the source of beatmaps
    /// </summary>
    /// <param name="query">Query string</param>
    /// <returns>List of first 20 beatmaps found</returns>
    [HttpGet]
    [Route("beatmap")]
    public async Task<ActionResult<SearchBeatmapResponse>> GetBeatmaps(string? query)
    {
        await GetOsuClaim();
        return Ok(await searchService.SearchBeatmaps(query));
    }
}
