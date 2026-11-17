using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    [ProducesResponseType(typeof(ReceivedRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReceivedRequestResponse>> GetRequests(int? playerId)
    {
        var id = playerId ?? throw new BadRequestException("Request player ID is null");

        var requests = await requestService.GetRequestsToUser(id);
        logger.LogInformation($"Found requests for {id}: {requests.Count}");
        return Ok(requests);
    }

    [HttpPost]
    [Authorize(Roles = "User,Proxy", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> PostRequest(PostRequestRequest request)
    {
        var sourceId = context.GetCurrentUserId(request);

        await requestService.CreateRequest(sourceId, request.DestinationId, request.BeatmapId).ThrowIfRequestError();

        return Ok();
    }
}
