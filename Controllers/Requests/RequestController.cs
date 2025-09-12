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
    private readonly Repository _repository;
    private readonly ILogger<RequestController> _logger;

    public RequestController(ILogger<RequestController> logger, Repository repository)
    {
        _logger = logger;
        _repository = repository;
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

        var requests = await _repository.GetRequestsToUser(id);
        _logger.LogInformation($"Found requests for {id}: {requests.Count}");
        return Ok(requests);
    }
}