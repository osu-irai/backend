using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using osuRequestor.Apis.OsuApi.Interfaces;
using osuRequestor.Controllers.Requests;
using osuRequestor.Data;
using osuRequestor.DTO.General;
using osuRequestor.DTO.Responses;

namespace osuRequestor.Controllers.Users;


[ApiController]
[Route("api/users/own")]
public class OwnUserController : ControllerBase
{
    private readonly DatabaseContext _databaseContext;
    private readonly IOsuApiProvider _osuApiProvider;
    private readonly ILogger<OwnUserController> _logger;

    private int? _claim()
    {
        var identity = HttpContext.User.Identity;
        if (identity is null)
        {
            return null;
        }
    
        var userId = identity.Name;
        return userId is null ? null : int.Parse(userId);
    }
    
    public OwnUserController(DatabaseContext databaseContext, IOsuApiProvider osuApiProvider, ILogger<OwnUserController> logger)
    {
        _databaseContext = databaseContext;
        _osuApiProvider = osuApiProvider;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(SelfUserResponse), 200)]
    public async Task<ActionResult<SelfUserResponse>> SelfUserGet()
    {
        var claim = _claim();
        if (claim is null)
        {
            _logger.LogWarning("Returning Forbid");
            return Forbid();
        }

        var user = await _databaseContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == claim);
        
        var requestCount = await _databaseContext.Requests.AsNoTracking().CountAsync(r => r.RequestedTo.Id == claim && !r.IsDeleted);

        return new SelfUserResponse
        {
            User = new UserDTO
            {
                Id = user!.Id,
                AvatarUrl = user!.AvatarUrl,
                Username = user!.Username
            },
            RequestCount = requestCount
        };
    }
}