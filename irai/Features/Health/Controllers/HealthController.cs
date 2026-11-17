using Microsoft.AspNetCore.Mvc;
using osuRequestor.Common.Controllers;

namespace osuRequestor.Features.Health.Controllers;

[ApiController]
[Route("health")]
public class HealthController : CrudController
{
    [HttpGet]
    public async Task<ActionResult<string>> GetHealth()
    {
        return Ok("ok");
    }
}
