using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using osuRequestor.Apis.OsuApi.Interfaces;
using osuRequestor.Data;
using osuRequestor.DTO.General;
using osuRequestor.DTO.Requests;
using osuRequestor.DTO.Responses;
using osuRequestor.Models;

namespace osuRequestor.Controllers.Requests;

[ApiController]
[Route("api/request")]
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
    [ProducesResponseType(typeof(ReceivedRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReceivedRequestResponse>> GetRequests(int? playerId)
    {
        if (playerId == null)
        {
            _logger.LogInformation($"Posted null ID");
            return BadRequest();
        }

        var requests = await _databaseContext
            .Requests
            // Tracking introduces unnecessary overhead for read-only ops
            .AsNoTracking()
            .Include(requestModel => requestModel.Beatmap)
            .Include(requestModel => requestModel.RequestedTo)
            .Where(req => req.RequestedTo.Id == playerId)
            .OrderByDescending(i => i.Id)
            .Select(x => new ReceivedRequestResponse
            {
                Id = x.Id,
                Beatmap = new BeatmapDTO
                {
                    BeatmapId = x.Beatmap.Id,
                    BeatmapsetId = x.Beatmap.BeatmapSet.Id,
                    Artist = x.Beatmap.BeatmapSet.Artist,
                    Title = x.Beatmap.BeatmapSet.Title,
                    Difficulty = x.Beatmap.Version,
                    Stars = x.Beatmap.StarRating
                },
                From = new UserDTO
                {
                    Id = x.RequestedFrom.Id,
                    Username = x.RequestedFrom.Username,
                    AvatarUrl = x.RequestedFrom.AvatarUrl
                }
            })
            .Take(50).ToListAsync();
        _logger.LogInformation($"Found requests for {playerId}: {requests.Count}");
        return Ok(requests);
    }
    


    // FIXME: Remove this bs when going to production LOL
    [HttpPost]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostRequest(
        [FromBody] PostRequestRequest postRequest)
    {
        // TODO: Change everything to use the record itself
        var (sourceId, destinationId, beatmapId) = postRequest;
        if (sourceId == null || destinationId == null || beatmapId == null) return BadRequest();
        var source = await _databaseContext.Users.FirstOrDefaultAsync(u => u.Id == sourceId);
        var destination = await _databaseContext.Users.FirstOrDefaultAsync(u => u.Id == destinationId);
        var beatmap = await _databaseContext.Beatmaps.FirstOrDefaultAsync(b => b.Id == beatmapId);

        if (source is null)
        {
            _logger.LogInformation("Could not find destination player: {SourceId}", sourceId);
            var apiResponse = await _osuApiProvider.GetUser(sourceId.Value);
            if (apiResponse is null)
            {
                _logger.LogWarning($"Destination player not found in osu!api: {sourceId}");
                return BadRequest();
            };
            _logger.LogInformation($"Found destination player: {sourceId} ({apiResponse.Username})");
            source = apiResponse.IntoModel();
            _databaseContext.Users.Add(source);
        }

        if (destination is null && destinationId != sourceId)
        {
            _logger.LogInformation($"Could not find destination player: {destinationId}");
            var apiResponse = await _osuApiProvider.GetUser(destinationId.Value);
            if (apiResponse is null)
            {
                _logger.LogWarning($"Destination player not found in osu!api: {destinationId}");
                return BadRequest();
            };
            _logger.LogInformation($"Found destination player: {destinationId} ({apiResponse.Username})");
            destination = apiResponse.IntoModel();
            _databaseContext.Users.Add(destination);
        }

        if (beatmap is null)
        {
            _logger.LogInformation($"Could not find beatmap: {beatmapId}");
            var apiResponse = await _osuApiProvider.GetBeatmap(beatmapId.Value);
            if (apiResponse is null)
            {
                _logger.LogWarning($"Beatmap not found in osu!api: {beatmapId}");
                return BadRequest();
            }
            _logger.LogInformation($"Found beatmap: {beatmapId}");
            beatmap = apiResponse.IntoModel();
            _databaseContext.Beatmaps.Add(beatmap);
        }
        
        await _databaseContext.SaveChangesAsync();
        var request = new RequestModel
        {
            Beatmap = beatmap,
            RequestedFrom = source,
            RequestedTo = destination,
        };
        _databaseContext.Requests.Add(request);
        await _databaseContext.SaveChangesAsync();
        _logger.LogInformation($"Created request for {destination.Id}");
        
        return Ok(request);
    }
}