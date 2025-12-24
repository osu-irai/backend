using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using IAuthenticationService = osuRequestor.Features.Authentication.Services.IAuthenticationService;
using osuRequestor.Infrastructure.Configuration;
using osuRequestor.Infrastructure.ExternalServices.OsuApi.Interfaces;
using osuRequestor.Infrastructure.ExternalServices.OsuApi.Models;

namespace osuRequestor.Features.Authentication.Controllers;

[ApiController]
[Route("api/oauth")]
public class OAuthController(
    ILogger<OAuthController> logger,
    IOsuApiProvider osuApiDataService,
    IAuthenticationService authenticationService,
    IOptions<ServerConfig> serverConfig)
    : ControllerBase
{
    private readonly ServerConfig _serverConfig = serverConfig.Value;

    /// <summary>
    /// osu! API authentication.
    /// </summary>
    [HttpGet("auth")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult Authenticate()
    {
        var authenticationProperties = new AuthenticationProperties
        {
            RedirectUri = this.Url.Action("CompleteAuthentication", "OAuth")
        };

        return Challenge(authenticationProperties, "osu");
    }

    [HttpGet("complete")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> CompleteAuthentication()
    {
        var authResult = await this.HttpContext.AuthenticateAsync("ExternalCookies");
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        logger.LogWarning("Expires at {ExpiresUtc}", authResult.Properties?.ExpiresUtc);

        var accessToken  = await this.HttpContext.GetTokenAsync("ExternalCookies", "access_token");
        var refreshToken = await this.HttpContext.GetTokenAsync("ExternalCookies", "refresh_token");

        User? user;
        try
        {
            user = await osuApiDataService.GetSelfUser(accessToken!);
        }
        catch (Exception e)
        {
            logger.LogError(e, "osu!api provider error!");
            return Forbid();
        }

        if (user is null || accessToken is null || refreshToken is null)
        {
            return Forbid();
        }

        var tokenExpiration = await authenticationService.SaveOsuLogin(user, accessToken, refreshToken);
        logger.LogInformation($"Token expires at {tokenExpiration}");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Id.ToString()),
            new Claim(ClaimTypes.Role, "User")
        };

        var id = new ClaimsIdentity(claims, "InternalCookies");
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc   = authResult.Properties?.ExpiresUtc
        };

        await this.HttpContext.SignInAsync("InternalCookies", new ClaimsPrincipal(id), authProperties);
        await this.HttpContext.SignOutAsync("ExternalCookies");

        logger.LogInformation("User {Username} logged in, toke expires on {TokenExpiration}", user.Username,
                              tokenExpiration);

        return Redirect(this._serverConfig.HomePage);
    }

    /// <summary>
    /// Sign out from current user.
    /// </summary>
    [Authorize]
    [HttpGet("signout")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Logout()
    {
        await this.HttpContext.SignOutAsync("InternalCookies");

        return Redirect(this._serverConfig.HomePage);
    }
}
