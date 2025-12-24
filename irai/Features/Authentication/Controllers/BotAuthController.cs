using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using osuRequestor.Features.Authentication.Models;
using osuRequestor.Infrastructure.Configuration;

namespace osuRequestor.Features.Authentication.Controllers;

[ApiController]
[Route("api/bot/auth")]
public class BotAuthController(
    ILogger<BotAuthController> logger,
    IOptions<AuthClientsConfig> clients,
    IOptions<AuthConfig> config)
    : ControllerBase
{
    private readonly AuthClientsConfig _clients = clients.Value;
    private readonly AuthConfig _config = config.Value;

    [HttpPost]
    public IActionResult Login([FromBody] TokenRequest auth)
    {
        if (!this._clients.TryGetValue(auth.Name, out var client) || client.ClientSecret != auth.ClientSecret)
        {
            return Unauthorized();
        }

        logger.LogInformation("Logging in user {AuthName}", auth.Name);
        var key          = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this._config.SecretKey));
        var signingCreds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(this._config.Issuer, $"{this._config.Audience}/{auth.Name.ToLower()}",
                                         expires: DateTime.Today.AddMinutes(this._config.ExpirationMinutes),
                                         signingCredentials: signingCreds);

        return Ok(new JwtSecurityTokenHandler().WriteToken(token));
    }
}
