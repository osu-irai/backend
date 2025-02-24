using osuRequestor.Apis.OsuApi.Models;

namespace osuRequestor.Apis.OsuApi.Interfaces;

public interface IOsuApiProvider
{
    Task<BeatmapScores?> GetBeatmapScores(int id, string[] mods);
    Task<Beatmap?> GetBeatmap(int id);
    Task<Score?> GetScore(long id);
    Task<ScoresResponse?> GetScores(long? cursor);
    Task<User?> GetUser(int id);
    Task<bool> DownloadMap(int id, string path);
}