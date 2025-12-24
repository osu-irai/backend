using Microsoft.EntityFrameworkCore;
using osuRequestor.Common.Models;
using osuRequestor.Features.Users.Models;
using osuRequestor.Infrastructure.Messaging;
using osuRequestor.Infrastructure.Notifications;
using osuRequestor.Infrastructure.Persistence;
using osuRequestor.Infrastructure.Persistence.Models;

namespace osuRequestor.Features.Users.Services;

public class OwnUserService(
    DatabaseContext databaseContext,
    ILogger<OwnUserService> logger,
    IRequestNotificationService notification,
    IMessageBusService messageBus) : IOwnUserService
{
    public async Task<SelfUserResponse> GetSelf(int claim)
    {
        var user = await databaseContext.Users.AsNoTracking()
                                        .FirstOrDefaultAsync(u => u.Id == claim);

        var requestCount = await databaseContext.Requests.AsNoTracking()
                                                .CountAsync(r => r.RequestedTo.Id == claim && !r.IsDeleted);

        return new SelfUserResponse
        {
            User = new UserDTO
            {
                Id        = user!.Id,
                AvatarUrl = user.AvatarUrl,
                Username  = user.Username
            },
            RequestCount = requestCount
        };
    }

    public async Task<SettingsDTO?> GetSettings(int claim)
    {
        var config = await databaseContext.Settings.AsNoTracking()
                                          .FirstOrDefaultAsync(u => u.UserId == claim);

        if (config is null)
        {
            return null;
        }

        return new SettingsDTO
        {
            EnableIrc    = config.EnableIrc,
            EnableTwitch = config.EnableTwitch
        };
    }

    public async Task UpdateSettings(int claim, SettingsDTO settings)
    {
        logger.LogInformation("Settings user settings");

        var storedConfig = await databaseContext.Settings.Where(s => s.UserId == claim)
                                                .FirstOrDefaultAsync();
        var user     = await databaseContext.Users.FirstOrDefaultAsync(u => u.Id      == claim);
        var twitch   = await databaseContext.Twitch.FirstOrDefaultAsync(u => u.UserId == claim);
        var username = user!.Username;
        if (storedConfig is not null)
        {
            logger.LogInformation("Found settings, updating");
            storedConfig.EnableIrc    = settings.EnableIrc;
            storedConfig.EnableTwitch = settings.EnableTwitch;
            await notification.NotifyAboutIrcChange(username, settings.EnableIrc);
            await messageBus.SubmitIrcSettingsChangeAsync(username, settings.EnableIrc);
            if (settings.EnableTwitch is not null && twitch is not null)
            {
                await messageBus.SubmitTwitchSettingsChangeAsync(twitch.TwitchId.ToString(), user.Id,
                                                                 settings.EnableTwitch.Value);
            }
        }
        else
        {
            logger.LogInformation("Creating new settings");
            databaseContext.Settings.Add(
                new SettingsModel
                {
                    EnableIrc = settings.EnableIrc,
                    UserId    = claim
                }
            );
        }

        await databaseContext.SaveChangesAsync();
    }

    public async Task<string?> GetTwitchUsername(int claim)
    {
        logger.LogInformation("Getting twitch settings");
        var twitch = await databaseContext.Twitch.FirstOrDefaultAsync(u => u.UserId == claim);

        if (twitch is null)
        {
            return null;
        }

        logger.LogInformation("Got username for {TwitchUsername}", twitch.Username);
        return twitch.Username;
    }
}
