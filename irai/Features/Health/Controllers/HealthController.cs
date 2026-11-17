using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using osuRequestor.Common.Controllers;

namespace osuRequestor.Features.Health.Controllers;

[ApiController]
[Route("health")]
public class HealthController : CrudController
{
    [HttpGet]
    public ContentHttpResult GetHealth()
    {
        return TypedResults.Text("ok");
    }
}
