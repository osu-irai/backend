using System.ComponentModel.DataAnnotations;

namespace irai.Features.Users.Persistence;

/// <summary>
/// Database model for user settings
/// </summary>
public class SettingsModel
{
    [Key]
    public int UserId { get; set; }

    public UserModel User { get; set; } = null!;

    public bool EnableIrc { get; set; } = false;

    public bool? EnableTwitch { get; set; } = false;

    public bool? FriendsOnly { get; set; } = true;
}
