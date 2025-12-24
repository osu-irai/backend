using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using osuRequestor.Common.Controllers;
using IAuthenticationService = osuRequestor.Features.Authentication.Services.IAuthenticationService;
using osuRequestor.Infrastructure.Configuration;
using osuRequestor.Infrastructure.ExternalServices.TwitchApi;

namespace osuRequestor.Features.Authentication.Controllers;

[ApiController]
[Route("api/twitch/auth")]
public class TwitchAuthController : CrudController
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<TwitchAuthController> _logger;
    private readonly ServerConfig _serverConfig;
    private readonly TwitchApiProvider _twitchApi;

    public TwitchAuthController(ILogger<TwitchAuthController> logger, IAuthenticationService authenticationService,
                                TwitchApiProvider twitchApi, IOptions<ServerConfig> config)
    {
        this._logger                = logger;
        this._authenticationService = authenticationService;
        this._twitchApi             = twitchApi;
        this._serverConfig          = config.Value;
    }

    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult Login()
    {
        var authenticationProperties = new AuthenticationProperties
        {
            RedirectUri = this.Url.Action("CompleteAuthentication", "TwitchAuth")
        };
        return Challenge(authenticationProperties, "Twitch");
    }


    [HttpGet("complete")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> CompleteAuthentication()
    {
        this._logger.LogInformation("Completed twitch auth");

        var success = await TryAuthenticateTwitch();
        if (!success)
        {
            return Unauthorized();
        }

        var id = await GetOsuClaim();
        this._logger.LogInformation("Authentication succeeded");

        var accessToken  = await this.HttpContext.GetTokenAsync("Twitch", "access_token");
        var refreshToken = await this.HttpContext.GetTokenAsync("Twitch", "refresh_token");
        if (accessToken is null || refreshToken is null)
        {
            this._logger.LogWarning("Failed to authenticate a Twitch user");
            return Unauthorized();
        }

        var twitchUserValidation = await this._twitchApi.ValidateUser(accessToken);
        if (twitchUserValidation is null)
        {
            this._logger.LogWarning("Failed to validate a Twitch user");
            return Unauthorized();
        }

        var twitchUser
            = await this._twitchApi.GetUser(twitchUserValidation.UserId, accessToken, twitchUserValidation.ClientId);

        var twitchId = int.Parse(twitchUserValidation.UserId);

        await this._authenticationService.SaveTwitchLogin(id, twitchId, twitchUser.Data[0].Login, accessToken,
                                                          refreshToken);

        return Redirect(this._serverConfig.HomePage);
    }
}
