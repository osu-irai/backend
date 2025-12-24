namespace osuRequestor.Features.Authentication.Models;

public class TokenResponse
{
    public string AccessToken { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
}
