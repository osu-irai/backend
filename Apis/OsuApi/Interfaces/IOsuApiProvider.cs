using osuRequestor.Apis.OsuApi.Models;

namespace osuRequestor.Apis.OsuApi.Interfaces;

public interface IOsuApiProvider
{
    Task<User?> GetSelfUser(string token);
    Task<User[]?> GetFriends(string token);
    Task<TokenResponse?> RefreshToken(string refreshToken, string accessToken);
}