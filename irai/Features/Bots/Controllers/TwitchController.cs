using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using osu.NET;
using osuRequestor.Common.Exceptions;
using osuRequestor.Features.Bots.Models;
using osuRequestor.Features.Bots.Services;
using osuRequestor.Features.Requests.Controllers;
using osuRequestor.Features.Requests.Models;
using osuRequestor.Features.Requests.Persistence;
using osuRequestor.Features.Requests.Services;
using osuRequestor.Infrastructure.Notifications;

namespace osuRequestor.Features.Bots.Controllers;

[ApiController]
[Route("twitch/info")]
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
    public async Task<Results<Ok<IList<TwitchDTO>>, UnauthorizedHttpResult, ForbidHttpResult>>
        GetAuthenticatedUsers()
    {
        return TypedResults.Ok(await botService.GetTwitchUsers());
    }

    [HttpPost]
    public async Task<Results<Ok, BadRequest, UnauthorizedHttpResult, ForbidHttpResult, NotFound, InternalServerError>>
        PostRequest([FromBody] PostBaseRequest baseRequest)
    {
        var (destinationId, beatmapId) = baseRequest;
        logger.LogInformation("Found {destinationName} and {beatmapId}", destinationId, beatmapId);

        var result =
            await requestSource.CreateRequest(null, destinationId!.Value, beatmapId!.Value, RequestSource.Twitch);

        if (result.Value is InternalError)
        {
            throw new ServerErrorException("Failed to create a request");
        }

        return TypedResults.Ok();
    }
}
