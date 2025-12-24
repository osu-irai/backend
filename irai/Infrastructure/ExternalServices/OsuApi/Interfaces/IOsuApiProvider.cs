using osuRequestor.Infrastructure.ExternalServices.OsuApi.Models;

namespace osuRequestor.Infrastructure.ExternalServices.OsuApi.Interfaces;

public interface IOsuApiProvider
{
    Task<User?> GetSelfUser(string token);
    Task<User[]?> GetFriends(string token);
    Task<TokenResponse?> RefreshToken(string refreshToken, string accessToken);
}
