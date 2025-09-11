using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using osu.NET;
using osuRequestor.Apis.OsuApi.Interfaces;
using osuRequestor.Data;
using osuRequestor.DTO.General;
using osuRequestor.DTO.Requests;
using osuRequestor.DTO.Responses;
using osuRequestor.Extensions;
using osuRequestor.Models;
using osuRequestor.Persistence;

namespace osuRequestor.Controllers.Requests;

[ApiController]
[Route("api/requests/own")]
public class OwnRequestController : ControllerBase
{
    private readonly Repository _repository;
    private readonly OsuApiClient _osuClient;
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
    
    public OwnRequestController(ILogger<RequestController> logger, OsuApiClient osuClient, Repository repository)
    {
        _logger = logger;
        _osuClient = osuClient;
        _repository = repository;
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

        var requests = await _repository.GetRequestsToUser(claim.Value);
        _logger.LogInformation($"Found requests for {claim}: {requests.Count}");
        return Ok(requests);
    }
    
    /// <summary>
    /// Creates a request to a player using your oauth token
    /// </summary> >
    /// <returns>HTTP 200 on success, HTTP 400 on missing parameters</returns>
    [HttpPost]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostRequest([FromBody] PostRequestWithName postBaseRequest)
    {
        var claim = _claim();
        if (claim is null)
        {
            return Forbid();
        }

        var (beatmapId, destinationName) = postBaseRequest;
        _logger.LogInformation("Found {destinationName} and {beatmapId}", destinationName, beatmapId);
        if (destinationName is null || beatmapId is null) return BadRequest();
        
        UserModel? source = await _repository.GetUserByClaim(claim.Value);
        UserModel? destination = await _repository.GetUserByName(destinationName);
        BeatmapModel? beatmap = await _repository.GetBeatmap(beatmapId); 
        
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
            _logger.LogInformation("Could not find destination player: {DestinationId}", destinationName);
            var apiResponse = await _osuClient.GetUserAsync(destinationName);
            if (apiResponse.IsFailure)
            {
                _logger.LogWarning("Destination player not found in osu!api: {DestinationId}", destinationName);
                return BadRequest();
            }
            var apiResponseSuccess = apiResponse.Value!;
            _logger.LogInformation("Found destination player: {DestinationId} ({ApiResponseUsername})", destinationName, apiResponseSuccess.Username);
            destination = apiResponseSuccess.ToModel();
        }
        else
        {
            _logger.LogInformation("Found destination player: {DestinationId}", destination.Id);
        }
        
        if (beatmap is null)
        {
            _logger.LogInformation("Could not find beatmap: {BeatmapId}", beatmapId);
            var apiResponse = await _osuClient.GetBeatmapAsync(beatmapId.Value);
            if (apiResponse.IsFailure)
            {
                _logger.LogWarning("Beatmap not found in osu!api: {BeatmapId}", beatmapId);
                return BadRequest();
            }
            _logger.LogInformation("Found beatmap: {BeatmapId}", beatmapId);
            var apiResponseSuccess = apiResponse.Value!;
            beatmap = apiResponseSuccess.ToModel();
        }        
        else
        {
            _logger.LogInformation("Found beatmap: {BeatmapId}", beatmap.Id);
        }
        var request = new RequestModel
        {
            Beatmap = beatmap,
            RequestedFrom = source,
            RequestedTo = destination,
        };
        await _repository.AddRequest(request);
        _logger.LogInformation($"Created request for {destination.Id}");
        
        return Ok();
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
        await _repository.DeleteRequest(requestId.Value);
        return Ok();
    }
}
