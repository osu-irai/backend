namespace osuRequestor.Configuration;

public class OsuApiConfig
{
    public const string Position = "osuApi";
    public int ClientId { get; set; }
    public string ClientSecret { get; set; } = null!;

    public string CallbackUrl { get; set; } = null!;
}