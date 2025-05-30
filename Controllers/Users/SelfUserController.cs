using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using osuRequestor.Apis.OsuApi.Interfaces;
using osuRequestor.Controllers.Requests;
using osuRequestor.Data;
using osuRequestor.DTO.General;
using osuRequestor.DTO.Responses;

namespace osuRequestor.Controllers.Users;


[ApiController]
[Route("api/users/self")]
public class SelfUserController : ControllerBase
{
    private readonly DatabaseContext _databaseContext;
    private readonly IOsuApiProvider _osuApiProvider;
    private readonly ILogger<RequestController> _logger;

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
    
    public SelfUserController(DatabaseContext databaseContext, IOsuApiProvider osuApiProvider, ILogger<RequestController> logger)
    {
        _databaseContext = databaseContext;
        _osuApiProvider = osuApiProvider;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<SelfUserResponse>> SelfUserGet()
    {
        var claim = _claim();
        if (claim is null)
        {
            return Forbid();
        }

        var user = await _databaseContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == claim);
        
        var requestCount = await _databaseContext.Requests.AsNoTracking().CountAsync(r => r.RequestedTo.Id == claim);

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