using osu.NET.Models.Beatmaps;
using osu.NET.Models.Users;
using osuRequestor.Apis.OsuApi.Models;
using osuRequestor.DTO.General;
using osuRequestor.Models;
using BeatmapSet = osu.NET.Models.Beatmaps.BeatmapSet;

namespace osuRequestor.Extensions;

public static class Mapper
{
   public static UserModel ToModel(this UserExtended user)
   {
      return new UserModel
      {
         Id = user.Id,
         Username = user.Username,
         CountryCode = user.CountryCode,
         AvatarUrl = user.AvatarUrl,
      };
   }

   public static BeatmapModel ToModel(this BeatmapExtended beatmap)
   {
      return new BeatmapModel
      {
         Id = beatmap.Id,
         BeatmapSet = beatmap.Set!.ToModel(),
         Version = beatmap.Version,
         ApproachRate = beatmap.ApproachRate,
         OverallDifficulty = beatmap.OverallDifficulty,
         CircleSize = beatmap.CircleSize,
         HealthDrain = beatmap.HealthDrain,
         BeatsPerMinute = beatmap.BPM,
         Circles = beatmap.CountCircles,
         Sliders = beatmap.CountSliders,
         Spinners = beatmap.CountSpinners,
         StarRating = beatmap.DifficultyRating,
         Status = beatmap.Status.IntoBeatmapStatus(),
         MaxCombo = beatmap.MaxCombo ?? 0,
         Mode = beatmap.Ruleset.IntoMode() 
      };
   }

   public static BeatmapSetModel ToModel(this BeatmapSet beatmapset)
   {
      return new BeatmapSetModel
      {
         Id = beatmapset.Id,
         Artist = beatmapset.Artist,
         Title = beatmapset.Title,
         CreatorId = beatmapset.CreatorId
      };
   }

   public static List<BeatmapDTO> ToBeatmapDtoList(this BeatmapSetsBundle bundle)
   {
      return bundle.Sets.SelectMany(s => s.Beatmaps!.Select(b => new BeatmapDTO
      {
         BeatmapId = b.Id,
         BeatmapsetId = s.Id,
         Artist = s.Artist,
         Title = s.Title,
         Difficulty = b.Version,
         Stars = b.DifficultyRating 
      })).Take(20).ToList();
   }
}