using System.Security.Principal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneOf.Monads;
using osu.NET;
using osuRequestor.Apis.OsuApi.Interfaces;
using osuRequestor.Data;
using osuRequestor.DTO.General;
using osuRequestor.DTO.Requests;
using osuRequestor.DTO.Responses;
using osuRequestor.Exceptions;
using osuRequestor.Extensions;
using osuRequestor.Models;
using osuRequestor.Persistence;
using osuRequestor.SignalR;

namespace osuRequestor.Controllers.Requests;

[ApiController]
[Route("api/requests/own")]
public class OwnRequestController : ControllerBase
{
    private readonly Repository _repository;
    private readonly OsuApiClient _osuClient;
    private readonly ILogger<OwnRequestController> _logger;
    private readonly IRequestNotificationService _notification;


    private int _claim() =>
        HttpContext.User.Identity.ThrowIfUnauthorized().OrOnNullName();
    public OwnRequestController(ILogger<OwnRequestController> logger, OsuApiClient osuClient, Repository repository, IRequestNotificationService notification)
    {
        _logger = logger;
        _osuClient = osuClient;
        _repository = repository;
        _notification = notification;
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
        var requests = await _repository.GetRequestsToUser(claim);
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

        var (beatmapId, destinationName) = postBaseRequest;
        _logger.LogInformation("Found {destinationName} and {beatmapId}", destinationName, beatmapId);
        if (destinationName is null || beatmapId is null) return BadRequest();
        
        Option<UserModel> source = await _repository.GetUserByClaim(claim);
        Option<UserModel> destination = await _repository.GetUserByName(destinationName);
        Option<BeatmapModel> beatmap = await _repository.GetBeatmap(beatmapId); 
        
        if (source.IsNone())
        {
            _logger.LogWarning("Could not find source player with token");
            return Unauthorized();
        }

        _logger.LogInformation("Found source player: {SourceId}", source.Value().Id);

        if (destination.IsNone())
        {
            _logger.LogInformation("Could not find destination player: {DestinationId}", destinationName);
            return NotFound("User {destinationName} was not found");
        }

        _logger.LogInformation("Found destination player: {DestinationId}", destination.Value().Id);

        if (beatmap.IsNone())
        {
            _logger.LogInformation("Could not find beatmap: {BeatmapId}", beatmapId);
            var apiResponse = await _osuClient.GetBeatmapAsync(beatmapId.Value)
                .BadGatewayOnFailure("Beatmap doesn't exist")
                .OrNotFound();
            _logger.LogInformation("Found beatmap: {BeatmapId}", beatmapId);
            beatmap = apiResponse.ToModel();
        }        
        else
        {
            _logger.LogInformation("Found beatmap: {BeatmapId}", beatmap.Value().Id);
        }
        var request = new RequestModel
        {
            Beatmap = beatmap.Value(),
            RequestedFrom = source.Value(),
            RequestedTo = destination.Value(),
        };
        await _repository.AddRequest(request);
        _logger.LogInformation("Created request for {Id}", destination.Value().Id);

        var reqNotif = new ReceivedRequestResponse
        {
            Id = request.Id,
            Beatmap = request.Beatmap.IntoDTO(),
            From = request.RequestedFrom.IntoDTO()
        };
        await _notification.NotifyUserAsync(destination.Value().Id, reqNotif);
        
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
        _claim();
        if (requestId is null) return BadRequest();
        await _repository.DeleteRequest(requestId.Value);
        return Ok();
    }
}
