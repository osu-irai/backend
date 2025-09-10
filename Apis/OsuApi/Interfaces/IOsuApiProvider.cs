using osu.NET.Models.Beatmaps;
using osuRequestor.Apis.OsuApi.Models;
using osuRequestor.Models;
using Beatmap = osuRequestor.Apis.OsuApi.Models.Beatmap;

namespace osuRequestor.Apis.OsuApi.Interfaces;

public interface IOsuApiProvider
{
    Task<User?> GetSelfUser(string token);
    Task<User[]?> GetFriends(string token);
    Task<TokenResponse?> RefreshToken(string refreshToken, string accessToken);
}