using irai.Common.Controllers;
using irai.Infrastructure.Configuration;
using irai.Infrastructure.ExternalServices.TwitchApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using IAuthenticationService = irai.Features.Authentication.Services.IAuthenticationService;
using Services_IAuthenticationService = irai.Features.Authentication.Services.IAuthenticationService;

namespace irai.Features.Authentication.Controllers;

[ApiController]
[Route("twitch/auth")]
public class TwitchAuthController : CrudController
{
    private readonly Services_IAuthenticationService _authenticationService;
    private readonly ILogger<TwitchAuthController> _logger;
    private readonly ServerConfig _serverConfig;
    private readonly TwitchApiProvider _twitchApi;

    public TwitchAuthController(ILogger<TwitchAuthController> logger, Services_IAuthenticationService authenticationService,
                                TwitchApiProvider twitchApi, IOptions<ServerConfig> config)
    {
        this._logger                = logger;
        this._authenticationService = authenticationService;
        this._twitchApi             = twitchApi;
        this._serverConfig          = config.Value;
    }

    [HttpGet]
    public ChallengeHttpResult Login()
    {
        var authenticationProperties = new AuthenticationProperties
        {
            RedirectUri = this.Url.Action("CompleteAuthentication", "TwitchAuth")
        };
        return TypedResults.Challenge(authenticationProperties, new[] { "Twitch" });
    }

    [HttpGet("complete")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<Results<RedirectHttpResult, UnauthorizedHttpResult>> CompleteAuthentication()
    {
        this._logger.LogInformation("Completed twitch auth");

        var success = await TryAuthenticateTwitch();
        if (!success)
        {
            return TypedResults.Unauthorized();
        }

        var id = await GetOsuClaim();
        this._logger.LogInformation("Authentication succeeded");

        var accessToken  = await this.HttpContext.GetTokenAsync("Twitch", "access_token");
        var refreshToken = await this.HttpContext.GetTokenAsync("Twitch", "refresh_token");
        if (accessToken is null || refreshToken is null)
        {
            this._logger.LogWarning("Failed to authenticate a Twitch user");
            return TypedResults.Unauthorized();
        }

        var twitchUserValidation = await this._twitchApi.ValidateUser(accessToken);
        if (twitchUserValidation is null)
        {
            this._logger.LogWarning("Failed to validate a Twitch user");
            return TypedResults.Unauthorized();
        }

        var twitchUser
            = await this._twitchApi.GetUser(twitchUserValidation.UserId, accessToken, twitchUserValidation.ClientId);

        var twitchId = int.Parse(twitchUserValidation.UserId);

        await this._authenticationService.SaveTwitchLogin(id, twitchId, twitchUser.Data[0].Login, accessToken,
                                                          refreshToken);

        return TypedResults.Redirect(this._serverConfig.HomePage);
    }
}
