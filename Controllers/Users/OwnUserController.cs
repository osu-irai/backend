using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using osuRequestor.Apis.TwitchApi;
using osuRequestor.Data;
using osuRequestor.DTO.General;
using osuRequestor.DTO.Responses;
using osuRequestor.ExceptionHandler.Exception;
using osuRequestor.Models;
using osuRequestor.Services;
using osuRequestor.SignalR;

namespace osuRequestor.Controllers.Users;

[ApiController]
[Route("api/users/own")]
public class OwnUserController(
    DatabaseContext databaseContext,
    ILogger<OwnUserController> logger,
    TwitchApiProvider twitchProvider,
    IRequestNotificationService notification,
    MessageBusService messageBus)
    : CrudController 
{

    [HttpGet]
    [ProducesResponseType(typeof(SelfUserResponse), 200)]
    public async Task<ActionResult<SelfUserResponse>> SelfUserGet()
    {
        var claim = await GetOsuClaim();

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
        var claim = await GetOsuClaim(); 

        var config = await databaseContext.Settings.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == claim);
        var twitch = await databaseContext.Twitch.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == claim);

        if (config is null) return null;

        return new SettingsDTO
        {
            EnableIrc = config.EnableIrc,
            EnableTwitch = config.EnableTwitch,
        };
    }

    [HttpPost]
    [Route("settings")]
    public async Task<IActionResult> SelfSetSettings([FromBody] SettingsDTO settings)
    {
        logger.LogInformation("Settings user settings");
        var claim = await GetOsuClaim(); 

        var storedConfig = await databaseContext.Settings.Where(s => s.UserId == claim).FirstOrDefaultAsync();
        var user = await databaseContext.Users.FirstOrDefaultAsync(u => u.Id == claim);
        var twitch = await databaseContext.Twitch.FirstOrDefaultAsync(u => u.UserId == claim);
        var username = user!.Username;
        if (storedConfig is not null)
        {
            logger.LogInformation("Found settings, updating");
            storedConfig.EnableIrc = settings.EnableIrc;
            storedConfig.EnableTwitch = settings.EnableTwitch;
            await notification.NotifyAboutIrcChange(username, settings.EnableIrc);
            await messageBus.SubmitIrcSettingsChangeAsync(username, settings.EnableIrc);
            if (settings.EnableTwitch is not null && twitch is not null)
            {
                await messageBus.SubmitTwitchSettingsChangeAsync(twitch.TwitchId.ToString(), settings.EnableTwitch.Value);
            }
        }
        else
        {
            logger.LogInformation("Creating new settings");
            databaseContext.Settings.Add(
                new SettingsModel
                {
                    EnableIrc = settings.EnableIrc,
                    UserId = claim
                }
            );
        }

        await databaseContext.SaveChangesAsync();
        return Ok();
    }

    [HttpGet]
    [Route("twitch")]
    public async Task<IActionResult> SelfGetTwitchUsername()
    {
        logger.LogInformation("Getting twitch settings");
        var claim = await GetOsuClaim();

        var twitch = await databaseContext.Twitch.FirstOrDefaultAsync(u => u.UserId == claim);
        if (twitch is null)
        {
            return Unauthorized();
        }
        logger.LogInformation("Got username for {TwitchUsername}", twitch?.Username);
        return Ok(twitch?.Username);
    }
}