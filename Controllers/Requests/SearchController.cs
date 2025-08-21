using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using osu.NET;
using osu.NET.Models.Beatmaps;
using osuRequestor.Data;
using osuRequestor.DTO.General;
using osuRequestor.DTO.Responses;
using osuRequestor.Persistence;

namespace osuRequestor.Controllers.Requests;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly Repository _repository;
    private readonly OsuApiClient _osuClient;
    private readonly ILogger<RequestController> _logger;

    public SearchController(ILogger<RequestController> logger, Repository repository, OsuApiClient osuClient)
    {
        _logger = logger;
        _repository = repository;
        _osuClient = osuClient;
    }

    /// <summary>
    /// Search for players whose nickname starts with <see cref="query"/> 
    /// </summary>
    /// <param name="query">Username query</param>
    /// <returns>List of usernames <see cref="SearchUserResponse"/></returns>
    [HttpGet]
    public async Task<ActionResult<SearchUserResponse>> GetPlayers(string? query)
    {
        _logger.LogInformation("Queried players: {Query}", query);
        var users = await _repository.QueryUsers(query);
        var response = new SearchUserResponse
        {
            Players = users.Select(u => u.IntoDTO()).ToList(),
            Count = users.Count
        };
        _logger.LogInformation("Players: {Player}, count: {Count}", response.Players[0], response.Count);
        return Ok(response);
    }

    [Route("beatmap")]
    public async Task<ActionResult<SearchBeatmapResponse>> GetBeatmaps(string? query)
    {
        
        _logger.LogInformation("Queried beatmaps: {Query}", query);
        var beatmaps = await _osuClient.SearchBeatmapSetsAsync(query ?? String.Empty);
        if (beatmaps.IsFailure)
        {
            _logger.LogWarning("Failed to fetch maps");
            return BadRequest();
        }

        var beatmapsChecked = beatmaps.Value!;
        var maps = beatmapsChecked.Sets.SelectMany(s => s.Beatmaps!.Select(b => new BeatmapDTO
        {
            BeatmapId = b.Id,
            BeatmapsetId = s.Id,
            Artist = s.Artist,
            Title = s.Title,
            Difficulty = b.Version,
            Stars = b.DifficultyRating 
        })).ToList();
        var response = new SearchBeatmapResponse
        {
            Beatmaps = maps,
            Count = maps.Count
        };
        _logger.LogInformation("Beatmaps: {Player}, count: {Count}", response.Beatmaps[0], response.Count);
        return Ok(response);
    }
}
