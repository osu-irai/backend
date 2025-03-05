using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using osuRequestor.Apis.OsuApi.Interfaces;
using osuRequestor.Apis.OsuApi.Models;
using osuRequestor.Data;
using osuRequestor.Models;

namespace osuRequestor.Controllers;

[ApiController]
[Route("api/oauth")]
public class OAuthController : Controller
{
    private readonly ILogger<OAuthController> _logger;
    private readonly IOsuApiProvider _apiProvider;
    private readonly DatabaseContext _databaseContext;

    public OAuthController(ILogger<OAuthController> logger, IOsuApiProvider apiProvider, DatabaseContext databaseContext)
    {
        _logger = logger;
        _apiProvider = apiProvider;
        _databaseContext = databaseContext;
    }

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
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> CompleteAuthentication()
    {
        var authResult = await HttpContext.AuthenticateAsync("ExternalCookies");
        if (!authResult.Succeeded)
            return Forbid();

        var accessToken = await HttpContext.GetTokenAsync("access_token");
        var refreshToken = await HttpContext.GetTokenAsync("refresh_token");

        User? user;

        try
        {
            user = await _apiProvider.GetSelfUser(accessToken!);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "osu!api provider error");
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
            });
        }
        else
        {
            _logger.LogInformation("User with id {Id} found", user.Id);
            existingUser.Username = user.Username;
            _databaseContext.Users.Update(existingUser);
        }

        var tokenExpiration =
            authResult.Properties?.ExpiresUtc?.DateTime.ToUniversalTime()
                              ?? DateTime.UtcNow.AddDays(1);

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
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Expires = tokenExpiration,
                UserId = user.Id
            });
        }

        await _databaseContext.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Id.ToString()),
        };

        var id = new ClaimsIdentity(claims, "InternalCookies");
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = authResult.Properties?.ExpiresUtc,
        };

        await HttpContext.SignInAsync("InternalCookies", new ClaimsPrincipal(id), authProperties);
        await HttpContext.SignOutAsync("ExternalCookies");

        _logger.LogInformation("User with id {Id} logged in", user.Id);

        return Redirect($"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/");
    }

    [Authorize]
    [HttpGet("logout")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("InternalCookies");

        return Redirect($"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/");
    }

}