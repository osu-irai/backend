using System.ComponentModel.DataAnnotations;

namespace osuRequestor.Models;

public class SettingsModel 
{
    [Key]
    public int UserId { get; set; }

    public UserModel User { get; set; } = null!;

    public bool EnableIrc { get; set; } = false;
}