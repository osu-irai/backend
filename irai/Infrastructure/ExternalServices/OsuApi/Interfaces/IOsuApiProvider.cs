using irai.Common.Extensions;
using irai.Infrastructure.ExternalServices.OsuApi.Models;

namespace irai.Infrastructure.ExternalServices.OsuApi.Interfaces;

public interface IOsuApiProvider
{
    Task<Option<User>> GetSelfUser(string token);
    Task<User[]> GetFriends(string token);
    Task<TokenResponse?> RefreshToken(string refreshToken, string accessToken);
}
