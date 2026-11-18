using irai.Common.Extensions;
using irai.Infrastructure.ExternalServices.OsuApi.Models;

namespace irai.Infrastructure.ExternalServices.OsuApi.Interfaces;

public interface IOsuApiProvider
{
    Task<Option<User>> GetSelfUser(string token);
    Task<User[]> GetFriends(string token);
    Task<TokenResponse?> RefreshToken(string refreshToken, string accessToken);

    /// <summary>
    /// App-level token for userless (in other words, oauth-less) calls.
    /// </summary>
    Task<string?> GetUserlessTokenAsync();
}
