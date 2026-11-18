using irai.Common.Extensions;
using irai.Features.Bots.Models;
using irai.Features.Bots.Services;
using irai.Features.Requests.Models;
using irai.Features.Requests.Persistence;
using irai.Features.Requests.Services;
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
    ILogger<TwitchController> logger,
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

        await requestSource.CreateRequest(null, destinationId!.Value, beatmapId!.Value, RequestSource.Twitch)
                            .ThrowIfRequestError();

        return TypedResults.Ok();
    }
}
