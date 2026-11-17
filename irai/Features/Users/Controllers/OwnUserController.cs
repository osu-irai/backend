using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using osuRequestor.Common.Controllers;
using osuRequestor.Features.Users.Models;
using osuRequestor.Features.Users.Services;

namespace osuRequestor.Features.Users.Controllers;

[ApiController]
[Route("users/own")]
public class OwnUserController(
    IOwnUserService userService)
    : CrudController
{
    [HttpGet]
    public async Task<Results<Ok<SelfUserResponse>, UnauthorizedHttpResult, InternalServerError>> SelfUserGet()
    {
        var claim = await GetOsuClaim();
        return TypedResults.Ok(await userService.GetSelf(claim));
    }

    [HttpGet]
    [Route("settings")]
    public async Task<Ok<SettingsDTO>> SelfGetSettings()
    {
        var claim = await GetOsuClaim();
        return TypedResults.Ok(await userService.GetSettings(claim));
    }

    [HttpPost]
    [Route("settings")]
    public async Task<Results<Ok, BadRequest, UnauthorizedHttpResult, InternalServerError>> SelfSetSettings(
        [FromBody] SettingsDTO settings)
    {
        var claim = await GetOsuClaim();
        await userService.UpdateSettings(claim, settings);
        return TypedResults.Ok();
    }

    [HttpGet]
    [Route("twitch")]
    public async Task<Results<ContentHttpResult, UnauthorizedHttpResult>> SelfGetTwitchUsername()
    {
        var claim    = await GetOsuClaim();
        var username = await userService.GetTwitchUsername(claim);
        return username is null ? TypedResults.Unauthorized() : TypedResults.Text(username);
    }
}
