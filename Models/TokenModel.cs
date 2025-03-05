using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using osuRequestor.Apis.OsuApi.Models;

namespace osuRequestor.Models;

public class TokenModel
{

    [Key]
    public int UserId { get; set; }

    public required string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public required DateTime Expires { get; set; }

    public UserModel User { get; set; } = null!;
}