using System.ComponentModel.DataAnnotations;

namespace osuRequestor.Infrastructure.Configuration;

public class AuthConfig
{
    public const string Position = "JwtSettings";

    [Required]
    public string SecretKey { get; set; } = null!;

    [Required]
    public string Audience { get; set; } = null!;

    [Required]
    public string Issuer { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int ExpirationMinutes { get; set; }
}
