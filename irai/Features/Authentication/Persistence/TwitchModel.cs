using System.ComponentModel.DataAnnotations;
using irai.Features.Users.Persistence;

namespace irai.Features.Authentication.Persistence;

/// <summary>
/// Database model for Twitch settings
/// </summary>
public class TwitchModel
{
    [Key]
    public int UserId { get; set; }

    public UserModel User { get; set; }

    public required int TwitchId { get; set; }
    public required string Username { get; set; }

    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
}
