using Microsoft.EntityFrameworkCore;
using osuRequestor.Features.Bots.Models;
using osuRequestor.Infrastructure.Persistence;

namespace osuRequestor.Features.Bots.Services;

public class BotService(
    DatabaseContext dbContext) : IBotService
{
    public async Task<IList<string>> GetIrcUsers()
    {
        return await dbContext.Users.Include(u => u.Settings)
                              .Where(u => u.Settings!.EnableIrc)
                              .Select(u => u.Username)
                              .ToListAsync();
    }

    public async Task<IList<TwitchDTO>> GetTwitchUsers()
    {
        return await dbContext.Twitch.Where(x =>
                                                x.User.Settings != null && x.User.Settings.EnableTwitch != null &&
                                                x.User.Settings.EnableTwitch.Value)
                              .Select(u => new TwitchDTO { TwitchId = u.TwitchId.ToString(), OsuId = u.UserId })
                              .ToListAsync();
    }
}
