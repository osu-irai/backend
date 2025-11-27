using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using osuRequestor.Data;

namespace osuRequestor.Controllers.Bots;

[ApiController]
[Route("api/bot/irc")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Irc")]
public class IrcBotController(DatabaseContext repository, ILogger<IrcBotController> logger)
    : ControllerBase
{
    private readonly ILogger<IrcBotController> _logger = logger;

    [HttpGet]
    public async Task<List<string>> GetNames()
    {
        return await repository.Users.Include(u => u.Settings).Where(u => u.Settings.EnableIrc).Select(u => u.Username)
            .ToListAsync();
    }
}