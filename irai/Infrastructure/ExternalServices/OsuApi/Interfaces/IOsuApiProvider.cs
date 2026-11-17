using osuRequestor.Common.Extensions;
using osuRequestor.Infrastructure.ExternalServices.OsuApi.Models;

namespace osuRequestor.Infrastructure.ExternalServices.OsuApi.Interfaces;

public interface IOsuApiProvider
{
    Task<Option<User>> GetSelfUser(string token);
    Task<User[]> GetFriends(string token);
    Task<TokenResponse?> RefreshToken(string refreshToken, string accessToken);
}
