using irai.Features.Bots.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace irai.Features.Bots.Controllers;

[ApiController]
[Route("bot/irc")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Irc")]
public class IrcBotController(
    IBotService botService)
    : ControllerBase
{
    [HttpGet]
    public async Task<Results<Microsoft.AspNetCore.Http.HttpResults.Ok<IList<string>>, UnauthorizedHttpResult, ForbidHttpResult>> GetNames()
    {
        return TypedResults.Ok(await botService.GetIrcUsers());
    }
}
