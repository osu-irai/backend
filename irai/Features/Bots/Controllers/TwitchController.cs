using irai.Common.Exceptions;
using irai.Features.Bots.Models;
using irai.Features.Bots.Services;
using irai.Features.Requests.Controllers;
using irai.Features.Requests.Models;
using irai.Features.Requests.Persistence;
using irai.Features.Requests.Services;
using irai.Infrastructure.Notifications;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using osu.NET;

namespace irai.Features.Bots.Controllers;

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
