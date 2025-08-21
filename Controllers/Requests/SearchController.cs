using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using osuRequestor.Data;
using osuRequestor.DTO.Responses;
using osuRequestor.Persistence;

namespace osuRequestor.Controllers.Requests;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly Repository _repository;
    private readonly ILogger<RequestController> _logger;

    public SearchController(ILogger<RequestController> logger, Repository repository)
    {
        _logger = logger;
        _repository = repository;
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
}