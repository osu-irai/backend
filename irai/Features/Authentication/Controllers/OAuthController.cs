using System.Security.Claims;
using irai.Common.Constants;
using irai.Common.Extensions;
using irai.Infrastructure.Configuration;
using irai.Infrastructure.ExternalServices.OsuApi.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using IAuthenticationService = irai.Features.Authentication.Services.IAuthenticationService;

namespace irai.Features.Authentication.Controllers;

[ApiController]
[Route("oauth")]
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
    public ChallengeHttpResult Authenticate()
    {
        var authenticationProperties = new AuthenticationProperties
        {
            RedirectUri = this.Url.Action("CompleteAuthentication", "OAuth")
        };

        return TypedResults.Challenge(authenticationProperties, new[] { "osu" });
    }

    [HttpGet("complete")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<Results<RedirectHttpResult, ForbidHttpResult>> CompleteAuthentication()
    {
        var authResult = await this.HttpContext.AuthenticateAsync("ExternalCookies");
        if (!authResult.Succeeded)
        {
            return TypedResults.Forbid();
        }

        logger.LogWarning("Expires at {ExpiresUtc}", authResult.Properties?.ExpiresUtc);

        var accessToken  = await this.HttpContext.GetTokenAsync("ExternalCookies", "access_token");
        var refreshToken = await this.HttpContext.GetTokenAsync("ExternalCookies", "refresh_token");
        if (accessToken is null || refreshToken is null)
        {
            return TypedResults.Forbid();
        }

        var apiUser = await osuApiDataService.GetSelfUser(accessToken);
        if (apiUser is None)
        {
            logger.LogError("osu!api provider error!");
            return TypedResults.Forbid();
        }

        var user = apiUser.Unwrap();

        var tokenExpiration = await authenticationService.SaveOsuLogin(user, accessToken, refreshToken);
        logger.LogInformation("Token expires at {TokenExpiration}", tokenExpiration);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Id.ToString()),
            new Claim(ClaimTypes.Role, Roles.USER)
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

        return TypedResults.Redirect(this._serverConfig.HomePage);
    }

    /// <summary>
    /// Sign out from current user.
    /// </summary>
    [Authorize(AuthenticationSchemes = "InternalCookies")]
    [HttpGet("signout")]
    public async Task<Results<RedirectHttpResult, UnauthorizedHttpResult>> Logout()
    {
        await this.HttpContext.SignOutAsync("InternalCookies");

        return TypedResults.Redirect(this._serverConfig.HomePage);
    }
}
