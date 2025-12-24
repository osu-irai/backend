using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using osuRequestor.Features.Bots.Services;
using osuRequestor.Infrastructure.Persistence;

namespace osuRequestor.Features.Bots.Controllers;

[ApiController]
[Route("api/bot/irc")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Irc")]
public class IrcBotController(
    IBotService botService
)
    : ControllerBase
{
    [HttpGet]
    public async Task<IList<string>> GetNames()
    {
        return await botService.GetIrcUsers();
    }
}
