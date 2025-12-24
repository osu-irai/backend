using osuRequestor.Infrastructure.ExternalServices.OsuApi.Models;
using osuRequestor.Infrastructure.Persistence.Models;

namespace osuRequestor.Tests.Infrastructure.Fixtures;

public static class TestDataBuilder
{
    public static UserModel User(int id, string username, bool enableIrc = false, bool enableTwitch = false)
    {
        return new UserModel
        {
            Id          = id,
            Username    = username,
            AvatarUrl   = $"https://a.ppy.sh/{id}",
            CountryCode = "US",
            Settings = new SettingsModel
            {
                EnableIrc    = enableIrc,
                EnableTwitch = enableTwitch
            }
        };
    }

    public static BeatmapSetModel BeatmapSet(int id, string artist = "Test Artist", string title = "Test Song")
    {
        return new BeatmapSetModel
        {
            Id        = id,
            Artist    = artist,
            Title     = title,
            CreatorId = 1
        };
    }

    public static BeatmapModel Beatmap(int id, BeatmapSetModel set, string difficulty = "Insane")
    {
        return new BeatmapModel
        {
            Id                = id,
            BeatmapSet        = set,
            Version           = difficulty,
            ApproachRate      = 9.4,
            OverallDifficulty = 9,
            CircleSize        = 4,
            HealthDrain       = 6,
            BeatsPerMinute    = 180,
            Circles           = 100,
            Sliders           = 50,
            Spinners          = 2,
            StarRating        = 6.5,
            Status            = BeatmapStatus.Ranked,
            MaxCombo          = 500,
            Mode              = Mode.Osu
        };
    }

    public static RequestModel Request(
        UserModel requestedTo,
        UserModel? requestedFrom,
        BeatmapModel beatmap,
        RequestSource source = RequestSource.Website)
    {
        return new RequestModel
        {
            Beatmap       = beatmap,
            RequestedTo   = requestedTo,
            RequestedFrom = requestedFrom,
            Source        = source,
            Date          = DateTime.UtcNow
        };
    }
}
