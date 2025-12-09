using System.ComponentModel.DataAnnotations;

namespace osuRequestor.Models;

/// <summary>
///     Database model for Twitch settings
/// </summary>
public class TwitchModel
{
    [Key] public int UserId { get; set; }

    public UserModel User { get; set; }

    public required int TwitchId { get; set; }
    public required string Username { get; set; }

    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
}