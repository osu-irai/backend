using Microsoft.AspNetCore.Mvc;
using osuRequestor.Common.Controllers;
using osuRequestor.Features.Users.Models;
using osuRequestor.Features.Users.Services;

namespace osuRequestor.Features.Users.Controllers;

[ApiController]
[Route("api/users/own")]
public class OwnUserController(
    IOwnUserService userService)
    : CrudController
{
    [HttpGet]
    [ProducesResponseType(typeof(SelfUserResponse), 200)]
    public async Task<ActionResult<SelfUserResponse>> SelfUserGet()
    {
        var claim = await GetOsuClaim();
        return await userService.GetSelf(claim);
    }

    [HttpGet]
    [Route("settings")]
    public async Task<ActionResult<SettingsDTO>?> SelfGetSettings()
    {
        var claim = await GetOsuClaim();
        return await userService.GetSettings(claim);
    }

    [HttpPost]
    [Route("settings")]
    public async Task<IActionResult> SelfSetSettings([FromBody] SettingsDTO settings)
    {
        var claim = await GetOsuClaim();
        await userService.UpdateSettings(claim, settings);
        return Ok();
    }

    [HttpGet]
    [Route("twitch")]
    public async Task<IActionResult> SelfGetTwitchUsername()
    {
        var claim = await GetOsuClaim();
        var username = await userService.GetTwitchUsername(claim);
        return username is null ? Unauthorized() : Ok(username);
    }
}
