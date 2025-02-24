using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc;
using osuRequestor.Apis.OsuApi.Interfaces;
using osuRequestor.Data;
using osuRequestor.Models;

namespace osuRequestor.Controllers;

[Route("api/requests")]
public class RequestController : ControllerBase
{
    private readonly DatabaseContext _databaseContext;
    private readonly IOsuApiProvider _osuApiProvider;

    public RequestController(DatabaseContext databaseContext, IOsuApiProvider osuApiProvider)
    {
        _databaseContext = databaseContext;
        _osuApiProvider = osuApiProvider;
    }
    /// <summary>
    /// Returns a list of beatmaps requested to a player
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRequests(int? playerId)
    {
        if (playerId != null)
        {
            var user = await _osuApiProvider.GetUser(playerId.Value);
            var res = HtmlEncoder.Default.Encode($"Beatmap {user?.Username}");
            return Ok(res);
        }
        else
        {
            return BadRequest();
        }
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