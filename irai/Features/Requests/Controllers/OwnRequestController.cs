using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneOf.Types;
using osu.NET;
using osuRequestor.Common.Controllers;
using osuRequestor.Common.Exceptions;
using osuRequestor.Features.Requests.Models;
using osuRequestor.Features.Requests.Services;

namespace osuRequestor.Features.Requests.Controllers;

[ApiController]
[Route("api/requests/own")]
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
    [Authorize(Roles = "User,Proxy", AuthenticationSchemes = "Bearer,InternalCookies")]
    public async Task<ActionResult<ReceivedRequestResponse>> GetSelfRequests()
    {
        logger.LogInformation("Received request list");
        var claim    = await GetOsuClaim();
        var requests = await requestService.GetRequestsToUser(claim);
        logger.LogInformation($"Found requests for {claim}: {requests.Count}");
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
    public async Task<IActionResult> PostRequest([FromBody] PostRequestWithName postBaseRequest)
    {
        var sourceId = await GetOsuClaim();

        var (beatmapId, destinationName) = postBaseRequest;

        logger.LogInformation("Found {destinationName} and {beatmapId}", destinationName, beatmapId);

        var result = await requestService.CreateRequest(sourceId, destinationName!, beatmapId!.Value);

        if (result.Value is Error)
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
