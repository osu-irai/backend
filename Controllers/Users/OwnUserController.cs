using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using osuRequestor.Data;
using osuRequestor.DTO.General;
using osuRequestor.DTO.Responses;
using osuRequestor.Models;
using osuRequestor.SignalR;

namespace osuRequestor.Controllers.Users;

[ApiController]
[Route("api/users/own")]
public class OwnUserController(
    DatabaseContext databaseContext,
    ILogger<OwnUserController> logger,
    IRequestNotificationService notification)
    : ControllerBase
{
    private int? _claim()
    {
        var identity = HttpContext.User.Identity;
        if (identity is null) return null;

        var userId = identity.Name;
        return userId is null ? null : int.Parse(userId);
    }

    [HttpGet]
    [ProducesResponseType(typeof(SelfUserResponse), 200)]
    public async Task<ActionResult<SelfUserResponse>> SelfUserGet()
    {
        var claim = _claim();
        if (claim is null)
        {
            logger.LogWarning("Returning Forbid");
            return Forbid();
        }

        var user = await databaseContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == claim);

        var requestCount = await databaseContext.Requests.AsNoTracking()
            .CountAsync(r => r.RequestedTo.Id == claim && !r.IsDeleted);

        return new SelfUserResponse
        {
            User = new UserDTO
            {
                Id = user!.Id,
                AvatarUrl = user.AvatarUrl,
                Username = user.Username
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
            logger.LogWarning("Returning Forbid");
            return Forbid();
        }

        var config = await databaseContext.Settings.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == claim);

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
        logger.LogInformation("Settings user settings");
        var claim = _claim();
        if (claim is null)
        {
            logger.LogWarning("Returning Forbid");
            return Forbid();
        }

        var storedConfig = await databaseContext.Settings.Where(s => s.UserId == claim).FirstOrDefaultAsync();
        var user = await databaseContext.Users.FirstOrDefaultAsync(u => u.Id == claim);
        var username = user!.Username;
        if (storedConfig is not null)
        {
            logger.LogInformation("Found settings, updating");
            storedConfig.EnableIrc = settings.EnableIrc;
            await notification.NotifyAboutIrcChange(username, settings.EnableIrc);
        }
        else
        {
            logger.LogInformation("Creating new settings");
            databaseContext.Settings.Add(
                new SettingsModel
                {
                    EnableIrc = settings.EnableIrc,
                    UserId = claim.Value
                }
            );
        }

        await databaseContext.SaveChangesAsync();
        return Ok();
    }
}