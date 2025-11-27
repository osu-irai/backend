namespace osuRequestor.Configuration;

public class AuthConfig
{
    public const string Position = "JwtSettings";
    public string SecretKey { get; set; }
    public string Audience { get; set; }
    public string Issuer { get; set; }
    public int ExpirationMinutes { get; set; }
}