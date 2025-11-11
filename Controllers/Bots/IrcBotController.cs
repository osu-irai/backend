using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using osuRequestor.Data;
using osuRequestor.Persistence;

namespace osuRequestor.Controllers.Bots;

[ApiController]
[Route("api/bot/irc")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Irc")]
public class IrcBotController : ControllerBase
{
    private readonly DatabaseContext _repository;
    private readonly ILogger<IrcBotController> _logger;

    public IrcBotController(DatabaseContext repository, ILogger<IrcBotController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<List<String>> GetNames()
    {
        return await _repository.Users.Select(u => u.Username).ToListAsync();
    }
}