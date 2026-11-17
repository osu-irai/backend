using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using osuRequestor.Common.Constants;
using osuRequestor.Common.Exceptions;
using osuRequestor.Common.Extensions;
using osuRequestor.Features.Requests.Models;
using osuRequestor.Features.Requests.Services;

namespace osuRequestor.Features.Requests.Controllers;

[ApiController]
[Route("request")]
public class RequestController(
    ILogger<RequestController> logger,
    IUserContext context,
    IRequestService requestService)
    : ControllerBase
{
    /// <summary>
    /// Returns a list of beatmaps requested to a player
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<Results<Ok<List<ReceivedRequestResponse>>, BadRequest, InternalServerError>> GetRequests(
        int? playerId)
    {
        var id = playerId ?? throw new BadRequestException("Request player ID is null");

        var requests = await requestService.GetRequestsToUser(id);
        logger.LogInformation($"Found requests for {id}: {requests.Count}");
        return TypedResults.Ok(requests);
    }

    [HttpPost]
    [Authorize(Roles = Roles.USER_OR_PROXY, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<Results<Ok, BadRequest, UnauthorizedHttpResult, ForbidHttpResult, NotFound, InternalServerError>>
        PostRequest(PostRequestRequest request)
    {
        var sourceId = context.GetCurrentUserId(request);

        await requestService.CreateRequest(sourceId, request.DestinationId, request.BeatmapId)
                            .ThrowIfRequestError();

        return TypedResults.Ok();
    }
}
