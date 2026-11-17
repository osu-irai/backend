using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using osu.NET;
using osuRequestor.Common.Constants;
using osuRequestor.Common.Controllers;
using osuRequestor.Common.Exceptions;
using osuRequestor.Features.Requests.Models;
using osuRequestor.Features.Requests.Services;

namespace osuRequestor.Features.Requests.Controllers;

[ApiController]
[Route("requests/own")]
public class OwnRequestController(
    ILogger<OwnRequestController> logger,
    IRequestService requestService)
    : CrudController
{
    /// <summary>
    /// Returns a list of beatmaps requested to a player using their oauth token
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(typeof(ReceivedRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize(Roles = Roles.USER_OR_PROXY, AuthenticationSchemes = "Bearer,InternalCookies")]
    public async Task<ActionResult<ReceivedRequestResponse>> GetSelfRequests()
    {
        logger.LogInformation("Received request list");
        var claim    = await GetOsuClaim();
        var requests = await requestService.GetRequestsToUser(claim);
        logger.LogInformation($"Found requests for {claim}: {requests.Count}");
        return Ok(requests);
    }

    /// <summary>
    /// Returns a list of beatmaps requested by the player using their oauth token
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("sent")]
    [ProducesResponseType(typeof(SentRequestResponse), StatusCodes.Status200OK)]
    [Authorize(Roles = Roles.USER_OR_PROXY, AuthenticationSchemes = "Bearer,InternalCookies")]
    public async Task<ActionResult<SentRequestResponse>> GetSentRequests()
    {
        var claim    = await GetOsuClaim();
        var requests = await requestService.GetRequestsByUser(claim);
        return Ok(requests);
    }

    /// <summary>
    /// Creates a request to a player using your oauth token
    /// </summary>
    /// <returns>HTTP 200 on success, HTTP 400 on missing parameters</returns>
    [HttpPost]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize(Roles = Roles.USER_OR_PROXY, AuthenticationSchemes = "Bearer,InternalCookies")]
    public async Task<IActionResult> PostRequest([FromBody] PostBaseRequest postBaseRequest)
    {
        var sourceId = await GetOsuClaim();

        var (destinationId, beatmapId) = postBaseRequest;

        logger.LogInformation("Found destination {destinationId} and map {beatmapId}", destinationId, beatmapId);

        var result = await requestService.CreateRequest(sourceId, destinationId!.Value, beatmapId!.Value);

        if (result.Value is InternalError)
        {
            throw new ServerErrorException("Failed to create a request");
        }

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
        await TryAuthenticateOsu();
        if (requestId is null)
        {
            return BadRequest();
        }

        await requestService.DeleteRequest(requestId.Value);
        return Ok();
    }
}
