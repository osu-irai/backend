using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using osuRequestor.Apis.TwitchApi;
using osuRequestor.Configuration;
using osuRequestor.Data;
using osuRequestor.ExceptionHandler.Exception;
using osuRequestor.Models;

namespace osuRequestor.Controllers;

[ApiController]
[Route("api/twitch/auth")]
public class TwitchAuthController : CrudController
{
    private readonly DatabaseContext _dbContext;
    private readonly ILogger<TwitchAuthController> _logger;
    private readonly TwitchApiProvider _twitchApi;
    private readonly ServerConfig _serverConfig;

    public TwitchAuthController(ILogger<TwitchAuthController> logger, DatabaseContext dbContext,
        TwitchApiProvider twitchApi, IOptions<ServerConfig> config)
    {
        _logger = logger;
        _dbContext = dbContext;
        _twitchApi = twitchApi;
        _serverConfig = config.Value;
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

        await TryAuthenticateTwitch();

        var id = await GetOsuClaim();
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

        var twitchUser = await _twitchApi.GetUser(twitchUserValidation.UserId, accessToken, twitchUserValidation.ClientId);

        var twitchId = int.Parse(twitchUserValidation.UserId);

        var twitchUserModel = await _dbContext.Twitch.FirstOrDefaultAsync(u => u.UserId == id);

        if (twitchUserModel is null)
        {
            _dbContext.Twitch.Add(new TwitchModel
            {
                UserId = id,
                Username = twitchUser.Data[0].Login,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TwitchId = twitchId
            });     
        }
        else
        {
            twitchUserModel.Username = twitchUser.Data[0].Login;
        }
        
        var user = await _dbContext.Settings.FirstOrDefaultAsync(u => u.UserId == id);
        if (user != null) user.EnableTwitch = true;
        await _dbContext.SaveChangesAsync();

        return Redirect(_serverConfig.HomePage);
    }
}