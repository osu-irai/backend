using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using osuRequestor.Features.Bots.Services;

namespace osuRequestor.Features.Bots.Controllers;

[ApiController]
[Route("bot/irc")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Irc")]
public class IrcBotController(
    IBotService botService)
    : ControllerBase
{
    [HttpGet]
    public async Task<Results<Ok<IList<string>>, UnauthorizedHttpResult, ForbidHttpResult>> GetNames()
    {
        return TypedResults.Ok(await botService.GetIrcUsers());
    }
}
