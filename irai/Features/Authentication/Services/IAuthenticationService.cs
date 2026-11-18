using irai.Infrastructure.ExternalServices.OsuApi.Models;

namespace irai.Features.Authentication.Services;

public interface IAuthenticationService
{
    Task<DateTime> SaveOsuLogin(User user, string accessToken, string refreshToken);

    Task SaveTwitchLogin(int osuId, int twitchId, string username, string accessToken, string refreshToken);
}
