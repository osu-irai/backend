using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using osuRequestor.ExceptionHandler.Exception;

namespace osuRequestor.Controllers;

/// <summary>
/// Base class for controllers
///
/// I want it to be unnecessary but I desperately need to
/// abstract away reauth for identities everywhere
/// </summary>
public class CrudController : ControllerBase
{
    protected async Task<int> GetOsuClaim()
    {
        var osuAuthResult = await HttpContext.AuthenticateAsync("InternalCookies");
        if (!osuAuthResult.Succeeded)
        {
            throw new UnauthorizedException();
        }

        var name = osuAuthResult.Principal.Identity?.Name;
        if (name is null || name.Length == 0)
        {
            throw new UnauthorizedException();
        }

        return int.Parse(name);
    }

    protected async Task TryAuthenticateOsu()
    {
        var authenticationResult = await HttpContext.AuthenticateAsync("InternalCookies");
        if (!authenticationResult.Succeeded)
        {
            throw new UnauthorizedException();
        }
    }

    protected async Task TryAuthenticateTwitch()
    {
        var authenticationResult =
            await HttpContext.AuthenticateAsync("Twitch");

        if (!authenticationResult.Succeeded)
        {
            throw new UnauthorizedException(authenticationResult.Failure?.Message ?? string.Empty);
        }
    }

}