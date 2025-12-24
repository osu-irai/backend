using osu.NET.Enums;

namespace osuRequestor.Infrastructure.ExternalServices.OsuApi.Models;

public enum Mode
{
    Osu = 0,
    Taiko = 1,
    Fruits = 2,
    Mania = 3
}

public static class ModeExtensions
{
    public static Mode IntoMode(this Ruleset ruleset)
    {
        return ruleset switch
        {
            Ruleset.Osu   => Mode.Osu,
            Ruleset.Taiko => Mode.Taiko,
            Ruleset.Catch => Mode.Fruits,
            Ruleset.Mania => Mode.Mania,
            var _         => throw new ArgumentOutOfRangeException(nameof(ruleset), ruleset, null)
        };
    }
}
