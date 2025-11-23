using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using osuRequestor.Apis.OsuApi.Interfaces;
using osuRequestor.Controllers.Requests;
using osuRequestor.Data;
using osuRequestor.DTO.General;
using osuRequestor.DTO.Responses;
using osuRequestor.Models;
using osuRequestor.SignalR;

namespace osuRequestor.Controllers.Users;


[ApiController]
[Route("api/users/own")]
public class OwnUserController : ControllerBase
{
    private readonly DatabaseContext _databaseContext;
    private readonly IOsuApiProvider _osuApiProvider;
    private readonly ILogger<OwnUserController> _logger;
    private readonly IRequestNotificationService _notification;

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
    
    public OwnUserController(DatabaseContext databaseContext, IOsuApiProvider osuApiProvider, ILogger<OwnUserController> logger, IRequestNotificationService notification)
    {
        _databaseContext = databaseContext;
        _osuApiProvider = osuApiProvider;
        _logger = logger;
        _notification = notification;
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

    [HttpGet]
    [Route("settings")]
    public async Task<ActionResult<SettingsDTO>?> SelfGetSettings()
    {
        var claim = _claim();
        if (claim is null)
        {
            _logger.LogWarning("Returning Forbid");
            return Forbid();
        }

        var config = await _databaseContext.Settings.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == claim);
        
        if (config is null) return null;

        return new SettingsDTO
        {
            EnableIrc = config.EnableIrc
        };
    }

    [HttpPost]
    [Route("settings")]
    public async Task<IActionResult> SelfSetSettings([FromBody] SettingsDTO settings)
    {
        _logger.LogInformation("Settings user settings");
        var claim = _claim();
        if (claim is null)
        {
            _logger.LogWarning("Returning Forbid");
            return Forbid();
        }

        var storedConfig = await _databaseContext.Settings.Where(s => s.UserId == claim).FirstOrDefaultAsync();
        var user = await _databaseContext.Users.FirstOrDefaultAsync(u => u.Id == claim);
        var username = user!.Username;
        if (storedConfig is not null)
        {
            _logger.LogInformation("Found settings, updating");
            storedConfig.EnableIrc = settings.EnableIrc;
            await _notification.NotifyAboutIrcChange(username, settings.EnableIrc);
        }
        else
        {
            _logger.LogInformation("Creating new settings");
            _databaseContext.Settings.Add(
                new SettingsModel
                {
                    EnableIrc = settings.EnableIrc,
                    UserId = claim.Value
                }
            );
        }

        await _databaseContext.SaveChangesAsync();
        return Ok();
    }
}