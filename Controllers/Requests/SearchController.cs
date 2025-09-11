using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using osu.NET;
using osu.NET.Models.Beatmaps;
using osuRequestor.Apis.OsuApi.Interfaces;
using osuRequestor.Data;
using osuRequestor.DTO.General;
using osuRequestor.DTO.Responses;
using osuRequestor.Extensions;
using osuRequestor.Models;
using osuRequestor.Persistence;

namespace osuRequestor.Controllers.Requests;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly Repository _repository;
    private readonly OsuApiClient _osuClient;
    private readonly IOsuApiProvider _osuProvider;
    private readonly ILogger<RequestController> _logger;

    public SearchController(ILogger<RequestController> logger, Repository repository, OsuApiClient osuClient, IOsuApiProvider osuProvider)
    {
        _logger = logger;
        _repository = repository;
        _osuClient = osuClient;
        _osuProvider = osuProvider;
    }

    private int? _claim()
    {
        var identity = HttpContext.User.Identity;
        if (identity is null)
        {
            return null;
        }
    
        var userId = identity.Name;
        return userId is null ? null : int.Parse(userId);
    }
    /// <summary>
    /// Search for players whose nickname starts with <see cref="query"/> 
    /// </summary>
    /// <param name="query">Username query</param>
    /// <returns>List of usernames <see cref="SearchUserResponse"/></returns>
    [HttpGet]
    [Route("player")]
    public async Task<ActionResult<SearchUserResponse>> GetPlayers(string? query)
    {
        _logger.LogInformation("Queried players: {Query}", query);
        var users = await _repository.QueryUsers(query);
        var response = new SearchUserResponse
        {
            Players = users.Select(u => u.IntoDTO()).ToList(),
            Count = users.Count
        };
        _logger.LogInformation("Players: {Player}, count: {Count}", response.Players.FirstOrDefault(), response.Count);
        return Ok(response);
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
        var claim = _claim();
        if (claim is null)
        {
            return Unauthorized(); 
        }

        var beatmaps = await _osuClient.SearchBeatmapSetsAsync(query ?? String.Empty);
        if (beatmaps.IsFailure)
        {
            _logger.LogWarning("Failed to fetch maps: {error}", beatmaps.Error);
            return BadRequest();
        }
        var maps = beatmaps.Value?.ToBeatmapDtoList();
        if (maps is null)
        {
            _logger.LogWarning("API returned no maps?");
            return BadRequest();
        }
        var response = new SearchBeatmapResponse
        {
            Beatmaps = maps,
            Count = maps.Count
        };
        _logger.LogInformation("Beatmaps: {FirstMap}, count: {Count}", response.Beatmaps.FirstOrDefault(), response.Count);
        return Ok(response);
    }
}
