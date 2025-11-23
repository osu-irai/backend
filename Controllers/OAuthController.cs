using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using osuRequestor.Apis.OsuApi.Interfaces;
using osuRequestor.Apis.OsuApi.Models;
using osuRequestor.Configuration;
using osuRequestor.Data;
using osuRequestor.Models;

namespace osuRequestor.Controllers;

[ApiController]
[Route("api/oauth")]
public class OAuthController : ControllerBase
{
    private readonly ILogger<OAuthController> _logger;
    private readonly IOsuApiProvider _osuApiDataService;
    private readonly DatabaseContext _databaseContext;
    private readonly ServerConfig _serverConfig;

    public OAuthController(ILogger<OAuthController> logger, 
    IOsuApiProvider osuApiDataService,
    DatabaseContext databaseContext, IOptions<ServerConfig> serverConfig)
    {
        _logger = logger;
        _osuApiDataService = osuApiDataService;
        _databaseContext = databaseContext;
        _serverConfig = serverConfig.Value;
    }

    /// <summary>
    ///     osu! API authentication.
    /// </summary>
    [HttpGet("auth")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult Authenticate()
    {
        var authenticationProperties = new AuthenticationProperties
        {
            RedirectUri = Url.Action("CompleteAuthentication", "OAuth")
        };

        return Challenge(authenticationProperties, "osu");
    }

    [HttpGet("complete")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> CompleteAuthentication()
    {
        var authResult = await HttpContext.AuthenticateAsync("ExternalCookies");
        if (!authResult.Succeeded)
        {
            return Forbid();
        }
        _logger.LogWarning($"Expires at {authResult.Properties?.ExpiresUtc}");

        var accessToken = await HttpContext.GetTokenAsync("ExternalCookies", "access_token");
        var refreshToken = await HttpContext.GetTokenAsync("ExternalCookies", "refresh_token");

        User? user;
        try
        {
            user = await _osuApiDataService.GetSelfUser(accessToken!);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "osu!api provider error!");
            return Forbid();
        }

        if (user is null || accessToken is null || refreshToken is null)
            return Forbid();
        
        var existingUser = await _databaseContext.Users.FindAsync(user.Id);
        if (existingUser is null)
        {
            _logger.LogInformation("User with id {Id} not found", user.Id);
            await _databaseContext.Users.AddAsync(new UserModel
            {
                Id = user.Id,
                Username = user.Username,
                AvatarUrl = user.AvatarUrl,
                CountryCode = user.CountryCode,
                Settings = new SettingsModel(),
            });
        }
        else
        {
            _logger.LogInformation("User with id {Id} found", user.Id);
            existingUser.Username = user.Username;
            existingUser.CountryCode = user.CountryCode;

            _databaseContext.Users.Update(existingUser);
        }

        var tokenExpiration = DateTime.UtcNow.AddDays(1);
        _logger.LogInformation($"Token expires at {tokenExpiration}");
        
        var existingTokens = await _databaseContext.Tokens.FindAsync(user.Id);
        if (existingTokens is not null)
        {
            existingTokens.AccessToken = accessToken;
            existingTokens.RefreshToken = refreshToken;
            existingTokens.Expires = tokenExpiration;

            _databaseContext.Tokens.Update(existingTokens);
        }
        else
        {
            await _databaseContext.Tokens.AddAsync(new TokenModel
            {
                UserId = user.Id,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Expires = tokenExpiration
            });
        }

        await _databaseContext.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Id.ToString()),
            new(ClaimTypes.Role, "User")
        };

        var id = new ClaimsIdentity(claims, "InternalCookies");
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = authResult.Properties?.ExpiresUtc
        };

        await HttpContext.SignInAsync("InternalCookies", new ClaimsPrincipal(id), authProperties);
        await HttpContext.SignOutAsync("ExternalCookies");

        _logger.LogInformation("User {Username} logged in, toke expires on {TokenExpiration}", user.Username, tokenExpiration);
        
        return Redirect(_serverConfig.HomePage);
    }

    /// <summary>
    ///     Sign out from current user.
    /// </summary>
    [Authorize]
    [HttpGet("signout")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("InternalCookies");

        return Redirect(_serverConfig.HomePage);
    }
}