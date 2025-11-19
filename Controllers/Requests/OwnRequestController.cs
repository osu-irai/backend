using System.Security.Principal;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneOf.Types;
using osu.NET;
using osuRequestor.Apis.OsuApi.Interfaces;
using osuRequestor.Data;
using osuRequestor.DTO.General;
using osuRequestor.DTO.Requests;
using osuRequestor.DTO.Responses;
using osuRequestor.ExceptionHandler.Exception;
using osuRequestor.Exceptions;
using osuRequestor.Extensions;
using osuRequestor.Models;
using osuRequestor.Persistence;
using osuRequestor.Services;
using osuRequestor.SignalR;

namespace osuRequestor.Controllers.Requests;

[ApiController]
[Route("api/requests/own")]
public class OwnRequestController : ControllerBase
{
    private readonly OsuApiClient _osuClient;
    private readonly DatabaseContext _dbContext;
    private readonly ILogger<OwnRequestController> _logger;
    private readonly IRequestNotificationService _notification;
    private readonly RequestService _requestService;
    


    private int _claim()
    {
        var identity = HttpContext.User.Identity;
            
        _logger.LogInformation("Identity is {identityName}", identity?.Name);
        return identity.ThrowIfUnauthorized().OrOnNullName();
    }

    public OwnRequestController(ILogger<OwnRequestController> logger, OsuApiClient osuClient, DatabaseContext dbContext, IRequestNotificationService notification, RequestService requestService)
    {
        _logger = logger;
        _osuClient = osuClient;
        _dbContext = dbContext;
        _notification = notification;
        _requestService = requestService;
    }
    
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
        _logger.LogInformation("Received request list");
        var claim = _claim();
        var requests = await _dbContext.GetRequestsToUser(claim);
        _logger.LogInformation($"Found requests for {claim}: {requests.Count}");
        return Ok(requests);
    }
    
    /// <summary>
    /// Creates a request to a player using your oauth token
    /// </summary> >
    /// <returns>HTTP 200 on success, HTTP 400 on missing parameters</returns>
    [HttpPost]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostRequest([FromBody] PostRequestWithName postBaseRequest)
    {
        var sourceId = _claim();
        
        var (beatmapId, destinationName) = postBaseRequest;
        if (destinationName is null || beatmapId is null) return BadRequest();
        _logger.LogInformation("Found {destinationName} and {beatmapId}", destinationName, beatmapId);
        
        var result = await _requestService.CreateRequest(sourceId, destinationName, beatmapId.Value);

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
        _claim();
        if (requestId is null) return BadRequest();
        await _dbContext.DeleteRequest(requestId.Value);
        return Ok();
    }
}
