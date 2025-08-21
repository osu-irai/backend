using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using osu.NET;
using osuRequestor.Apis.OsuApi.Interfaces;
using osuRequestor.Apis.OsuApi.Models;
using osuRequestor.Data;
using osuRequestor.DTO.General;
using osuRequestor.DTO.Requests;
using osuRequestor.DTO.Responses;
using osuRequestor.Models;
using osuRequestor.Persistence;

namespace osuRequestor.Controllers.Requests;

[ApiController]
[Route("api/request")]
public class RequestController : ControllerBase
{
    private readonly Repository _repository;
    private readonly OsuApiClient _osuClient;
    private readonly ILogger<RequestController> _logger;

    public RequestController(ILogger<RequestController> logger, OsuApiClient osuClient, Repository repository)
    {
        _logger = logger;
        _osuClient = osuClient;
        _repository = repository;
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
        if (playerId is null)
        {
            _logger.LogInformation($"Posted null ID");
            return BadRequest();
        }

        var requests = await _repository.GetRequestsToUser(playerId.Value);
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
        UserModel? source = await _repository.GetUser(sourceId);
        UserModel? destination = await _repository.GetUser(destinationId);
        BeatmapModel? beatmap = await _repository.GetBeatmap(beatmapId);

        if (source is null)
        {
            _logger.LogInformation("Could not find destination player: {SourceId}", sourceId);
            var apiResponse = await _osuClient.GetUserAsync(sourceId.Value);
            if (apiResponse.IsFailure)
            {
                _logger.LogWarning($"Destination player not found in osu!api: {sourceId}");
                return BadRequest();
            };
            var apiResponseSuccess = apiResponse.Value!;
            _logger.LogInformation($"Found destination player: {sourceId} ({apiResponseSuccess.Username})");
            source = UserModel.FromUserExtended(apiResponseSuccess);
            await _repository.AddUser(source);
        }

        if (destination is null && destinationId != sourceId)
        {
            _logger.LogInformation($"Could not find destination player: {destinationId}");
            var apiResponse = await _osuClient.GetUserAsync(destinationId.Value);
            if (apiResponse.IsFailure)
            {
                _logger.LogWarning($"Destination player not found in osu!api: {destinationId}");
                return BadRequest();
            };
            var apiResponseSuccess = apiResponse.Value!;
            _logger.LogInformation($"Found destination player: {destinationId} ({apiResponseSuccess.Username})");
            destination = UserModel.FromUserExtended(apiResponseSuccess);
            await _repository.AddUser(destination);
        }

        if (beatmap is null)
        {
            _logger.LogInformation($"Could not find beatmap: {beatmapId}");
            var apiResponse = await _osuClient.GetBeatmapAsync(beatmapId.Value);
            if (apiResponse.IsFailure)
            {
                _logger.LogWarning($"Beatmap not found in osu!api: {beatmapId}");
                return BadRequest();
            }
            var apiResponseSuccess = apiResponse.Value!;
            _logger.LogInformation($"Found beatmap: {beatmapId}");
            beatmap = BeatmapModel.FromBeatmapExtended(apiResponseSuccess);
            await _repository.AddBeatmap(beatmap);
        }
        
        var request = new RequestModel
        {
            Beatmap = beatmap,
            RequestedFrom = source,
            RequestedTo = destination,
        };
        await _repository.AddRequest(request);
        _logger.LogInformation($"Created request for {destination.Id}");
        
        return Ok(request);
    }
}