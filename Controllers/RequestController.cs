using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc;
using osuRequestor.Data;
using osuRequestor.Models;

namespace osuRequestor.Controllers;

[Route("api/requests")]
public class RequestController : ControllerBase
{
    private readonly DatabaseContext _databaseContext;

    public RequestController(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }
    /// <summary>
    /// Returns a list of beatmaps requested to a player
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetRequests(int? playerId)
    {
        var res = HtmlEncoder.Default.Encode(playerId == null ? "Beatmap" : $"Beatmap {playerId}");
        return Ok(res);
    }
    /// <summary>
    /// Creates a request to a player
    /// </summary>
    /// <param name="playerId">ID of a player the request is addressed to</param>
    /// <param name="beatmapId">ID of the beatmap requested</param>
    /// <returns>HTTP 200 on success, HTTP 400 on missing parameters</returns>
    [HttpPost]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult PostRequest(int? playerId, int? beatmapId)
    {
        if (playerId == null || beatmapId == null)
        {
            return BadRequest();
        }

        return Ok();
    } 
}