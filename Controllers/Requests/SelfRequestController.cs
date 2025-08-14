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
[Route("api/requests/self")]
public class SelfRequestController : ControllerBase
{
    private readonly DatabaseContext _databaseContext;
    private readonly IOsuApiProvider _osuApiProvider;
    private readonly ILogger<RequestController> _logger;

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
    
    public SelfRequestController(DatabaseContext databaseContext, IOsuApiProvider osuApiProvider, ILogger<RequestController> logger)
    {
        _databaseContext = databaseContext;
        _osuApiProvider = osuApiProvider;
        _logger = logger;
    }
    
    /// <summary>
    /// Returns a list of beatmaps requested to a player using their oauth token
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(typeof(ReceivedRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReceivedRequestResponse>> GetSelfRequests()
    {
        var claim = _claim();
        if (claim is null)
        {
            return Forbid();
        }

        var requests = await _databaseContext
            .Requests
            // Tracking introduces unnecessary overhead for read-only ops
            .AsNoTracking()
            .Include(requestModel => requestModel.Beatmap)
            .Include(requestModel => requestModel.RequestedTo)
            .Where(req => req.RequestedTo.Id == claim)
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
        _logger.LogInformation($"Found requests for {claim}: {requests.Count}");
        return Ok(requests);
    }


    /// <summary>
    /// Creates a request to a player using your oauth token
    /// </summary> >
    /// <param name="postSelfRequest"><see cref="PostSelfRequestRequest"/></param>
    /// <returns>HTTP 200 on success, HTTP 400 on missing parameters</returns>
    [HttpPost]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostSelfRequest(
        [FromBody] PostSelfRequestRequest postSelfRequest)
    {
        var (destinationId, beatmapId) = postSelfRequest;
        if (destinationId == null || beatmapId == null) return BadRequest();
        var claim = _claim();
        if (claim is null)
        {
            return Forbid();
        }
        UserModel? source = await _databaseContext.Tokens
            .Where(s => s.UserId == claim)
            .Select(s => s.User)
            .FirstOrDefaultAsync();
        UserModel? destination = await _databaseContext.Users.FirstOrDefaultAsync(u => u.Id == destinationId);
        BeatmapModel? beatmap = await _databaseContext.Beatmaps.FirstOrDefaultAsync(b => b.Id == beatmapId);
        
        if (source is null)
        {
            _logger.LogWarning($"Could not find source player with token");
            return Forbid();
        }
        else
        {
            _logger.LogInformation("Found source player: {SourceId}", source.Id);
        }
        
        if (destination is null)
        {
            _logger.LogInformation("Could not find destination player: {DestinationId}", destinationId);
            var apiResponse = await _osuApiProvider.GetUser(destinationId.Value);
            if (apiResponse is null)
            {
                _logger.LogWarning("Destination player not found in osu!api: {DestinationId}", destinationId);
                return BadRequest();
            };
            _logger.LogInformation("Found destination player: {DestinationId} ({ApiResponseUsername})", destinationId, apiResponse.Username);
            destination = apiResponse.IntoModel();
            _databaseContext.Users.Add(destination);
        }
        else
        {
            _logger.LogInformation("Found destination player: {DestinationId}", destination.Id);
        }
        
        if (beatmap is null)
        {
            _logger.LogInformation("Could not find beatmap: {BeatmapId}", beatmapId);
            var apiResponse = await _osuApiProvider.GetBeatmap(beatmapId.Value);
            if (apiResponse is null)
            {
                _logger.LogWarning("Beatmap not found in osu!api: {BeatmapId}", beatmapId);
                return BadRequest();
            }
            _logger.LogInformation("Found beatmap: {BeatmapId}", beatmapId);
            beatmap = apiResponse.IntoModel();
            _databaseContext.Beatmaps.Add(beatmap);
        }        
        else
        {
            _logger.LogInformation("Found beatmap: {BeatmapId}", beatmap.Id);
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

    /// <summary>
    /// Deletes a request by ID
    /// </summary>
    /// <param name="requestId">request ID to delete</param>
    /// <returns>200/400</returns>
    [HttpDelete]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteSelfRequest(int? requestId)
    {
        if (_claim() is null) return Forbid();
        if (requestId is null) return BadRequest();
        var request = await _databaseContext.Requests.FirstOrDefaultAsync(r => r.Id == requestId);
        if (request is null) return BadRequest();
        _databaseContext.Requests.Remove(request);
        await _databaseContext.SaveChangesAsync();
        return Ok();
    }
}
