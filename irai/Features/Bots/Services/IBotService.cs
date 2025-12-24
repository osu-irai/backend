using osuRequestor.Features.Bots.Models;

namespace osuRequestor.Features.Bots.Services;

public interface IBotService
{
    public Task<IList<string>> GetIrcUsers();
    public Task<IList<TwitchDTO>> GetTwitchUsers();
}
