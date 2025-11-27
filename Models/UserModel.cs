using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using osuRequestor.DTO.General;
using User = osuRequestor.Apis.OsuApi.Models.User;

namespace osuRequestor.Models;

/// <summary>
///     Database model for a registered user
/// </summary>
[Index(nameof(Username))]
public class UserModel
{
    [Key] public int Id { get; set; }

    [Length(3, 20)] public string Username { get; set; } = null!;

    [Length(2, 2)] public string CountryCode { get; set; } = "??";

    [Length(30, 50)] public string AvatarUrl { get; set; } = null!;

    public TokenModel Token { get; set; } = null!;

    public SettingsModel? Settings { get; set; } = null;

    public TwitchModel? TwitchSettings { get; set; } = null;

    public User IntoApiModel()
    {
        return new User
        {
            Id = Id,
            Username = Username,
            CountryCode = CountryCode,
            AvatarUrl = AvatarUrl
        };
    }

    public UserDTO IntoDTO()
    {
        return new UserDTO
        {
            Id = Id,
            AvatarUrl = AvatarUrl,
            Username = Username
        };
    }
}