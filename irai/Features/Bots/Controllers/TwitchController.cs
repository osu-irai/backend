using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneOf.Types;
using osu.NET;
using osuRequestor.Common.Exceptions;
using osuRequestor.Features.Bots.Models;
using osuRequestor.Features.Bots.Services;
using osuRequestor.Features.Requests.Controllers;
using osuRequestor.Features.Requests.Models;
using osuRequestor.Features.Requests.Services;
using osuRequestor.Infrastructure.Notifications;
using osuRequestor.Infrastructure.Persistence;
using osuRequestor.Infrastructure.Persistence.Models;

namespace osuRequestor.Features.Bots.Controllers;

[ApiController]
[Route("api/twitch/info")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Twitch")]
public class TwitchController(
    IRequestService requestSource,
    OsuApiClient osuClient,
    ILogger<OwnRequestController> logger,
    IRequestNotificationService notification,
    IBotService botService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IList<TwitchDTO>> GetAuthenticatedUsers()
    {
        return await botService.GetTwitchUsers();
    }

    [HttpPost]
    public async Task<IActionResult> PostRequest([FromBody] PostBaseRequest baseRequest)
    {
        var (destinationId, beatmapId) = baseRequest;
        logger.LogInformation("Found {destinationName} and {beatmapId}", destinationId, beatmapId);

        var result =
            await requestSource.CreateRequest(null, destinationId!.Value, beatmapId!.Value, RequestSource.Twitch);

        if (result.Value is Error)
        {
            throw new ServerErrorException("Failed to create a request");
        }

        return Ok();
    }
}
