using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneOf.Monads;
using osuRequestor.Apis.OsuApi.Interfaces;
using osuRequestor.Apis.OsuApi.Models;
using osuRequestor.Data;
using osuRequestor.DTO.General;
using osuRequestor.DTO.Requests;
using osuRequestor.DTO.Responses;
using osuRequestor.ExceptionHandler.Exception;
using osuRequestor.Exceptions;
using osuRequestor.Extensions;
using osuRequestor.Models;
using osuRequestor.Persistence;

namespace osuRequestor.Controllers.Requests;

[ApiController]
[Route("api/request")]
public class RequestController : ControllerBase
{
    private readonly ILogger<RequestController> _logger;
    private readonly DatabaseContext _dbContext;

    public RequestController(ILogger<RequestController> logger, DatabaseContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }
    
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

        var requests = await _dbContext.GetRequestsToUser(id);
        _logger.LogInformation($"Found requests for {id}: {requests.Count}");
        return Ok(requests);
    }
}