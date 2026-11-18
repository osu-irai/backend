using irai.Features.Users.Models;

namespace irai.Features.Users.Services;

public interface IOwnUserService
{
    Task<SelfUserResponse> GetSelf(int claim);
    Task<SettingsDTO?> GetSettings(int claim);
    Task UpdateSettings(int claim, SettingsDTO settings);
    Task<string?> GetTwitchUsername(int claim);
}
