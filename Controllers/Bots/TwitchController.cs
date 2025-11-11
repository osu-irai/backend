using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Toolkit.HighPerformance.Helpers;
using OneOf.Monads;
using osu.NET;
using osuRequestor.Controllers.Requests;
using osuRequestor.Data;
using osuRequestor.DTO.Requests;
using osuRequestor.DTO.Responses;
using osuRequestor.Exceptions;
using osuRequestor.Extensions;
using osuRequestor.Models;
using osuRequestor.Persistence;
using osuRequestor.SignalR;

namespace osuRequestor.Controllers.Bots;

[ApiController]
[Route("api/bot/twitch")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Twitch")]
public class TwitchController : ControllerBase
{
    private readonly DatabaseContext _dbContext;
    private readonly OsuApiClient _osuClient;
    private readonly ILogger<OwnRequestController> _logger;
    private readonly IRequestNotificationService _notification;

    public TwitchController(DatabaseContext dbContext, OsuApiClient osuClient, ILogger<OwnRequestController> logger, IRequestNotificationService notification)
    {
        _dbContext = dbContext;
        _osuClient = osuClient;
        _logger = logger;
        _notification = notification;
    }

    [HttpGet]
    public async Task<IActionResult> GetTest()
    {
        _logger.LogCritical("Works");
        return Ok("Works!");
    }

    [HttpPost]
    public async Task<IActionResult> PostRequest([FromBody] PostBaseRequest baseRequest)
    {
        var (beatmapId, destinationId) = baseRequest;
        _logger.LogInformation("Found {destinationName} and {beatmapId}", destinationId, beatmapId);
        if (destinationId is null || beatmapId is null) return BadRequest();
        
        Option<UserModel> destination = await _dbContext.GetUser(destinationId);
        Option<BeatmapModel> beatmap = await _dbContext.GetBeatmap(beatmapId); 
        
        if (destination.IsNone())
        {
            _logger.LogInformation("Could not find destination player: {DestinationId}", destinationId);
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
            RequestedFrom = null, 
            RequestedTo = destination.Value(),
            Source = RequestSource.Twitch,
        };
        await _dbContext.AddRequest(request);
        _logger.LogInformation("Created request for {Id}", destination.Value().Id);

        var reqNotif = new ReceivedRequestResponse
        {
            Id = request.Id,
            Beatmap = request.Beatmap.IntoDTO(),
            From = null, 
            Source = RequestSource.Twitch
        };
        await _notification.NotifyUserAsync(destination.Value().Id, reqNotif);
        
        return Ok();
    }
}