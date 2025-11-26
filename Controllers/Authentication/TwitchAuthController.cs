using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using osuRequestor.Configuration;

namespace osuRequestor.Controllers;

[ApiController]
[Route("api/twitch/auth")]
public class TwitchAuthController : ControllerBase
{
    private readonly ILogger<TwitchAuthController> _logger;
    private readonly HttpClient _httpClient;
    private readonly TwitchConfig _config;

    public TwitchAuthController(ILogger<TwitchAuthController> logger, IHttpClientFactory httpClientFactory, IOptions<TwitchConfig> config)
    {
        _logger = logger;
        _config = config.Value;
        _httpClient = httpClientFactory.CreateClient();
    }

    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult Login()
    {
        var authenticationProperties = new AuthenticationProperties
        {
            RedirectUri = Url.Action("CompleteAuthentication", "TwitchAuth"),
        };
        
        _logger.LogWarning("Redirect URL: {redirectUrl}", authenticationProperties.RedirectUri);

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
            _logger.LogInformation("Authentication failed: {authenticationResult}", authenticationResult.Failure?.Message);
            return Unauthorized();
        }
        
        _logger.LogInformation("Authentication succeeded");

        // Now retrieve the saved tokens from authentication properties
        var accessToken = await HttpContext.GetTokenAsync("Twitch", "access_token");
        var refreshToken = await HttpContext.GetTokenAsync("Twitch", "refresh_token");
        var expiresAt = await HttpContext.GetTokenAsync("Twitch", "expires_at");
        var tokenType = await HttpContext.GetTokenAsync("Twitch", "token_type");

        return Ok();
    }

    [HttpGet("callback")]
    public async Task<IActionResult> TwitchCallback()
    {
        _logger.LogInformation("Received twitch callback");
        return Ok();
    }
}