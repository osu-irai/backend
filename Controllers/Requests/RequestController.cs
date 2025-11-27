using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using osuRequestor.Data;
using osuRequestor.DTO.Requests;
using osuRequestor.DTO.Responses;
using osuRequestor.ExceptionHandler.Exception;
using osuRequestor.Persistence;
using osuRequestor.Services;

namespace osuRequestor.Controllers.Requests;

[ApiController]
[Route("api/request")]
public class RequestController(
    ILogger<RequestController> logger,
    DatabaseContext dbContext,
    IUserContext context,
    RequestService request)
    : ControllerBase
{
    /// <summary>
    ///     Returns a list of beatmaps requested to a player
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(typeof(ReceivedRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReceivedRequestResponse>> GetRequests(int? playerId)
    {
        var id = playerId ?? throw new BadRequestException("Request player ID is null");

        var requests = await dbContext.GetRequestsToUser(id);
        logger.LogInformation($"Found requests for {id}: {requests.Count}");
        return Ok(requests);
    }

    [HttpPost]
    [Authorize(Roles = "User,Proxy", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> PostRequest(PostRequestRequest request1)
    {
        var sourceId = context.GetCurrentUserId(request1);

        var result = await request.CreateRequest(sourceId, request1.DestinationId, request1.BeatmapId);

        return result.Match<IActionResult>(ok => Ok(), bnf => NotFound(), unf => NotFound(),
            uperr => throw new BadGatewayException("osu!api error"),
            interr => throw new ServerErrorException("internal error"));
    }
}