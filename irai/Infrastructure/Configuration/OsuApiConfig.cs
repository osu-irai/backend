using System.ComponentModel.DataAnnotations;

namespace osuRequestor.Infrastructure.Configuration;

public class OsuApiConfig
{
    public const string Position = "osuApi";

    [Range(1, int.MaxValue)]
    public int ClientId { get; set; }

    [Required]
    public string ClientSecret { get; set; } = null!;

    [Required]
    public string CallbackUrl { get; set; } = null!;
}
