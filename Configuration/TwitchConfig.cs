namespace osuRequestor.Configuration;

public class TwitchConfig
{
    public const string Position = "Twitch";

    public string ClientId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!;
    public string RedirectUrl { get; set; } = null!;
}