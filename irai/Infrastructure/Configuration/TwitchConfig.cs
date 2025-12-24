using System.ComponentModel.DataAnnotations;

namespace osuRequestor.Infrastructure.Configuration;

public class TwitchConfig
{
    public const string Position = "Twitch";

    [Required]
    public string ClientId { get; set; } = null!;

    [Required]
    public string ClientSecret { get; set; } = null!;

    [Required]
    public string RedirectUrl { get; set; } = null!;
}
