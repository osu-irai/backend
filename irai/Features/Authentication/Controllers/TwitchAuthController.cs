using irai.Common.Controllers;
using irai.Infrastructure.Configuration;
using irai.Infrastructure.ExternalServices.TwitchApi;
using irai.Infrastructure.ExternalServices.TwitchApi.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using IAuthenticationService = irai.Features.Authentication.Services.IAuthenticationService;

namespace irai.Features.Authentication.Controllers;

[ApiController]
[Route("twitch/auth")]
public class TwitchAuthController(
    ILogger<TwitchAuthController> logger,
    IAuthenticationService authenticationService,
    TwitchApiProvider twitchApi,
    IOptions<ServerConfig> config) : CrudController
{
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
        logger.LogInformation("Completed twitch auth");

        var success = await TryAuthenticateTwitch();
        if (!success)
        {
            return TypedResults.Unauthorized();
        }

        var id = await GetOsuClaim();
        logger.LogInformation("Authentication succeeded");

        var accessToken  = await this.HttpContext.GetTokenAsync("Twitch", "access_token");
        var refreshToken = await this.HttpContext.GetTokenAsync("Twitch", "refresh_token");
        if (accessToken is null || refreshToken is null)
        {
            logger.LogWarning("Failed to authenticate a Twitch user");
            return TypedResults.Unauthorized();
        }

        var validationResult = await twitchApi.ValidateUser(accessToken);
        if (validationResult is not OkVariant<TwitchValidation> { Value: var twitchUserValidation })
        {
            logger.LogWarning("Failed to validate a Twitch user");
            return TypedResults.Unauthorized();
        }

        var userResult = await twitchApi.GetUser(twitchUserValidation.UserId, accessToken,
                                                 twitchUserValidation.ClientId);
        if (userResult is not OkVariant<TwitchUserList> { Value: var twitchUser } ||
            twitchUser.Data.FirstOrDefault()
                      ?.Login is not { } twitchLogin)
        {
            logger.LogWarning("Failed to fetch a Twitch user");
            return TypedResults.Unauthorized();
        }

        var twitchId = int.Parse(twitchUserValidation.UserId);

        await authenticationService.SaveTwitchLogin(id, twitchId, twitchLogin, accessToken,
                                                    refreshToken);

        return TypedResults.Redirect(config.Value.HomePage);
    }
}
