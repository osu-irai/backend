using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using osuRequestor.Data;
using osuRequestor.DTO.Responses;

namespace osuRequestor.Controllers.Requests;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly DatabaseContext _databaseContext;
    private readonly ILogger<RequestController> _logger;

    public SearchController(DatabaseContext databaseContext, ILogger<RequestController> logger)
    {
        _databaseContext = databaseContext;
        _logger = logger;
    }

    /// <summary>
    /// Search for players whose nickname starts with <see cref="query"/> 
    /// </summary>
    /// <param name="query">Username query</param>
    /// <returns>List of usernames <see cref="SearchUserResponse"/></returns>
    [HttpGet]
    public ActionResult<SearchUserResponse> GetPlayers(string? query)
    {
        _logger.LogInformation("Queried players: {Query}", query);
        var users = _databaseContext
            .Users
            .AsNoTracking()
            .Where(u => u.Username.StartsWith(query ?? String.Empty))
            .OrderBy(u => u.Username)
            .Take(10)
            .ToList();
        var response = new SearchUserResponse
        {
            Players = users.Select(u => u.IntoDTO()).ToList(),
            Count = users.Count
        };
        _logger.LogInformation("Players: {Player}, count: {Count}", response.Players[0], response.Count);
        return Ok(response);
    }
}