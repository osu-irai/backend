using System.ComponentModel.DataAnnotations;

namespace osuRequestor.Models;

/// <summary>
///     Database model for user settings
/// </summary>
public class SettingsModel
{
    [Key] public int UserId { get; set; }

    public UserModel User { get; set; } = null!;

    public bool EnableIrc { get; set; } = false;
}