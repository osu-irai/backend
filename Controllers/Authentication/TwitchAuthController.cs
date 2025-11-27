using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using osuRequestor.Apis.TwitchApi;
using osuRequestor.Data;
using osuRequestor.ExceptionHandler.Exception;
using osuRequestor.Models;

namespace osuRequestor.Controllers;

[ApiController]
[Route("api/twitch/auth")]
public class TwitchAuthController : ControllerBase
{
    private readonly DatabaseContext _dbContext;
    private readonly ILogger<TwitchAuthController> _logger;
    private readonly TwitchApiProvider _twitchApi;

    public TwitchAuthController(ILogger<TwitchAuthController> logger, DatabaseContext dbContext,
        TwitchApiProvider twitchApi)
    {
        _logger = logger;
        _dbContext = dbContext;
        _twitchApi = twitchApi;
    }


    private async Task<int> _osuId()
    {
        var osuAuthResult = await HttpContext.AuthenticateAsync("InternalCookies");
        if (!osuAuthResult.Succeeded)
        {
            _logger.LogInformation("osu auth failed: {osuAuthResult}", osuAuthResult.Failure?.Message);
            throw new UnauthorizedException();
        }

        var name = osuAuthResult.Principal.Identity?.Name;
        if (name is null || name.Length == 0)
        {
            _logger.LogInformation("osu identity name is null");
            throw new UnauthorizedException();
        }

        return int.Parse(name);
    }

    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult Login()
    {
        var authenticationProperties = new AuthenticationProperties
        {
            RedirectUri = Url.Action("CompleteAuthentication", "TwitchAuth")
        };
        return Challenge(authenticationProperties, "Twitch");
    }


    [HttpGet("complete")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> CompleteAuthentication()
    {
        _logger.LogInformation("Completed twitch auth");

        var authenticationResult =
            await HttpContext.AuthenticateAsync("Twitch");

        if (!authenticationResult.Succeeded)
        {
            _logger.LogInformation("Authentication failed: {authenticationResult}",
                authenticationResult.Failure?.Message);
            return Unauthorized();
        }

        var id = await _osuId();
        _logger.LogInformation("Authentication succeeded");

        var accessToken = await HttpContext.GetTokenAsync("Twitch", "access_token");
        var refreshToken = await HttpContext.GetTokenAsync("Twitch", "refresh_token");
        if (accessToken is null || refreshToken is null)
        {
            _logger.LogWarning("Failed to authenticate a Twitch user");
            return Unauthorized();
        }

        var twitchUserValidation = await _twitchApi.ValidateUser(accessToken);
        if (twitchUserValidation is null)
        {
            _logger.LogWarning("Failed to validate a Twitch user");
            return Unauthorized();
        }

        var twitchId = int.Parse(twitchUserValidation.UserId);

        _dbContext.Twitch.Add(new TwitchModel
        {
            UserId = id,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            IsEnabled = true,
            TwitchId = twitchId
        });
        await _dbContext.SaveChangesAsync();

        return Ok();
    }
}