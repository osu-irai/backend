using irai.Common.Controllers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace irai.Features.Health.Controllers;

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
