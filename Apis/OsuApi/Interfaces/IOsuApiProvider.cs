using osuRequestor.Apis.OsuApi.Models;
using osuRequestor.Models;

namespace osuRequestor.Apis.OsuApi.Interfaces;

public interface IOsuApiProvider
{
    Task<BeatmapScores?> GetBeatmapScores(int id, string[] mods);
    Task<Beatmap?> GetBeatmap(int id);
    Task<Score?> GetScore(long id);
    Task<ScoresResponse?> GetScores(long? cursor);
    Task<User?> GetUser(int id);
    Task<User?> GetSelfUser(string token);
    Task<bool> DownloadMap(int id, string path);
    Task<User[]?> GetFriends(string token);
    Task<TokenResponse?> RefreshToken(string refreshToken, string accessToken);
}