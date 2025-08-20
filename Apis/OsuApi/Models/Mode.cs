using osu.NET.Enums;

namespace osuRequestor.Apis.OsuApi.Models
{
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
                Ruleset.Osu => Mode.Osu,
                Ruleset.Taiko => Mode.Taiko,
                Ruleset.Catch => Mode.Fruits,
                Ruleset.Mania => Mode.Mania,
                _ => throw new ArgumentOutOfRangeException(nameof(ruleset), ruleset, null)
            };
        }
    }
}
