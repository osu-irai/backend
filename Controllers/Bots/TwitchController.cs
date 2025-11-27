using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneOf.Types;
using osu.NET;
using osuRequestor.Controllers.Requests;
using osuRequestor.Data;
using osuRequestor.DTO.Requests;
using osuRequestor.ExceptionHandler.Exception;
using osuRequestor.Models;
using osuRequestor.Services;
using osuRequestor.SignalR;

namespace osuRequestor.Controllers.Bots;

[ApiController]
[Route("api/twitch/info")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Twitch")]
public class TwitchController(
    RequestService requestSource,
    OsuApiClient osuClient,
    ILogger<OwnRequestController> logger,
    IRequestNotificationService notification,
    DatabaseContext dbContext)
    : ControllerBase
{
    [HttpGet]
    public async Task<List<TwitchModel>> GetAuthenticatedUsers()
    {
        return await dbContext.Twitch.Where(x => x.IsEnabled).ToListAsync();
    }

    [HttpPost]
    public async Task<IActionResult> PostRequest([FromBody] PostBaseRequest baseRequest)
    {
        var (beatmapId, destinationId) = baseRequest;
        logger.LogInformation("Found {destinationName} and {beatmapId}", destinationId, beatmapId);
        if (destinationId is null || beatmapId is null) return BadRequest();
        var result =
            await requestSource.CreateRequest(null, destinationId.Value, beatmapId.Value, RequestSource.Twitch);

        if (result.Value is Error) throw new ServerErrorException("Failed to create a request");

        return Ok();
    }
}