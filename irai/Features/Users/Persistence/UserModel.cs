using System.ComponentModel.DataAnnotations;
using irai.Common.Models;
using irai.Features.Authentication.Persistence;
using Microsoft.EntityFrameworkCore;

namespace irai.Features.Users.Persistence;

/// <summary>
/// Database model for a registered user
/// </summary>
[Index(nameof(Username))]
public class UserModel
{
    [Key]
    public int Id { get; set; }

    [Length(3, 20)]
    public string Username { get; set; } = null!;

    [Length(2, 2)]
    public string CountryCode { get; set; } = "??";

    [Length(30, 50)]
    public string AvatarUrl { get; set; } = null!;

    public TokenModel Token { get; set; } = null!;

    public SettingsModel? Settings { get; set; } = null;

    public TwitchModel? TwitchSettings { get; set; } = null;

    public ICollection<FriendModel> Friends { get; set; } = [];

    public UserDTO IntoDTO()
    {
        return new UserDTO
        {
            Id        = this.Id,
            AvatarUrl = this.AvatarUrl,
            Username  = this.Username
        };
    }
}
