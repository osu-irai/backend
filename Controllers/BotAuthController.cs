using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using osuRequestor.Configuration;
using osuRequestor.DTO.Auth;

namespace osuRequestor.Controllers;

[ApiController]
[Route("api/bot/auth")]
public class BotAuthController : ControllerBase
{
    private readonly ILogger<BotAuthController> _logger;
    private readonly Dictionary<string, AuthClient> _clients;
    private readonly AuthConfig _config;

    public BotAuthController(ILogger<BotAuthController> logger, IOptions<Dictionary<string, AuthClient>> clients, IOptions<AuthConfig> config)
    {
        _logger = logger;
        _clients = clients.Value;
        _config = config.Value;
    }

    [HttpPost]
    public IActionResult Login([FromBody] TokenRequest auth)
    {
        if (_clients[auth.Name].ClientSecret != auth.ClientSecret)
        {
            return Unauthorized();
        }

        _logger.LogInformation($"Logging in user {auth.Name}");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.SecretKey));
        var signingCreds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(issuer: _config.Issuer, audience: $"{_config.Audience}/{auth.Name.ToLower()}", 
            expires: DateTime.Today.AddMinutes(_config.ExpirationMinutes), signingCredentials:signingCreds);

        return Ok(new JwtSecurityTokenHandler().WriteToken(token));
    }
}