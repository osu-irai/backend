using osu.NET.Enums;
using osuRequestor.Apis.OsuApi.Models;


namespace osuRequestor.Apis.OsuApi.Models
{
    public enum BeatmapStatus
    {
        Graveyard,
        Wip,
        Pending,
        Ranked,
        Approved,
        Qualified,
        Loved
    }

    static class BeatmapStatusExtension
    {
        public static BeatmapStatus IntoBeatmapStatus(this RankedStatus status)
        {
            return status switch
            {
                RankedStatus.Graveyard => BeatmapStatus.Graveyard,
                RankedStatus.WIP => BeatmapStatus.Wip,
                RankedStatus.Pending => BeatmapStatus.Pending,
                RankedStatus.Ranked => BeatmapStatus.Ranked,
                RankedStatus.Approved => BeatmapStatus.Approved,
                RankedStatus.Qualified => BeatmapStatus.Qualified,
                RankedStatus.Loved => BeatmapStatus.Loved,
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
            };
        }
    }
}
