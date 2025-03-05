using System.Diagnostics;
using System.Text.Encodings.Web;
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.Entity.Migrations;
using osuRequestor.Apis.OsuApi.Interfaces;
using osuRequestor.Apis.OsuApi.Models;
using osuRequestor.Data;
using osuRequestor.Models;

namespace osuRequestor.Controllers;

[Route("api/requests")]
public class RequestController : ControllerBase
{
    private readonly DatabaseContext _databaseContext;
    private readonly IOsuApiProvider _osuApiProvider;
    private readonly ILogger<RequestController> _logger;

    public RequestController(DatabaseContext databaseContext, IOsuApiProvider osuApiProvider, ILogger<RequestController> logger)
    {
        _databaseContext = databaseContext;
        _osuApiProvider = osuApiProvider;
        _logger = logger;
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
        if (playerId == null) return BadRequest();

        var requests = await _databaseContext
            .Requests
            // Tracking introduces unnecessary overhead for read-only ops
            .AsNoTracking()
            .Include(requestModel => requestModel.Beatmap)
            .Include(requestModel => requestModel.RequestedTo)
            .OrderByDescending(i => i.Id)
            .Take(10).ToListAsync();
        if (requests.Count == 0)
        {
            _logger.LogInformation("No requests found");
            return NotFound();
        }
        _logger.LogInformation($"Found requests for {playerId}: {requests.Count}");
        return Ok(requests);
    }

    /// <summary>
    /// Creates a request to a player
    /// </summary>
    /// <param name="sourceId">ID of the player making a request</param>
    /// <param name="destinationId">ID of the player receiving the request</param>
    /// <param name="beatmapId">ID of the beatmap requested</param>
    /// <returns>HTTP 200 on success, HTTP 400 on missing parameters</returns>
    [HttpPost]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostRequest(int? sourceId,
        int? destinationId,
        int? beatmapId)
    {
        if (sourceId == null || destinationId == null || beatmapId == null) return BadRequest();

        var source = await _databaseContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == sourceId);
        var dest = await _databaseContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == destinationId);
        var beatmap = await _databaseContext.Beatmaps.AsNoTracking().FirstOrDefaultAsync(b => b.Id == beatmapId);

        if (source == null)
        {
            _logger.LogWarning($"Could not find source player: {sourceId}");
            var apiResponse = await _osuApiProvider.GetUser(sourceId.Value);
            if (apiResponse is null)
            {
                _logger.LogWarning($"Player not found in osu!api: {sourceId}");
                return BadRequest();
            };
            _logger.LogWarning($"Found source player: {sourceId} ({apiResponse.Username})");
            source = apiResponse.IntoModel();
            _databaseContext.Users.Add(source);
        }

        if (dest == null)
        {
            _logger.LogWarning($"Could not find destination player: {destinationId}");
            var apiResponse = await _osuApiProvider.GetUser(destinationId.Value);
            if (apiResponse is null)
            {
                _logger.LogWarning($"Destination player not found in osu!api: {destinationId}");
                return BadRequest();
            };
            _logger.LogWarning($"Found destination player: {destinationId} ({apiResponse.Username})");
            dest = apiResponse.IntoModel();
            _databaseContext.Users.Add(dest);
        }

        if (beatmap == null)
        {
            _logger.LogWarning($"Could not find beatmap: {beatmapId}");
            var apiResponse = await _osuApiProvider.GetBeatmap(beatmapId.Value);
            if (apiResponse is null)
            {
                _logger.LogWarning($"Beatmap not found in osu!api: {beatmapId}");
                return BadRequest();
            }
            _logger.LogWarning($"Found beatmap: {beatmapId}");
            beatmap = apiResponse.IntoModel();
            _databaseContext.Beatmaps.Add(beatmap);
        }
        await _databaseContext.SaveChangesAsync();
        var request = new RequestModel
        {
            Beatmap = beatmap,
            RequestedFrom = source,
            RequestedTo = dest,
        };
        _databaseContext.Requests.Add(request);
        await _databaseContext.SaveChangesAsync();
        _logger.LogInformation($"Created request for {sourceId}");

        return Ok(request);
    }
}