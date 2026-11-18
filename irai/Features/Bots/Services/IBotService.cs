using irai.Features.Bots.Models;

namespace irai.Features.Bots.Services;

public interface IBotService
{
    public Task<IList<string>> GetIrcUsers();
    public Task<IList<TwitchDTO>> GetTwitchUsers();
}
