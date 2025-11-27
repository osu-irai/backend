using System.ComponentModel.DataAnnotations;

namespace osuRequestor.Models;

/// <summary>
///     Database model for osu! authentication tokens
/// </summary>
public class TokenModel
{
    [Key] public int UserId { get; set; }

    public required string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public required DateTime Expires { get; set; }

    public UserModel User { get; set; } = null!;
}