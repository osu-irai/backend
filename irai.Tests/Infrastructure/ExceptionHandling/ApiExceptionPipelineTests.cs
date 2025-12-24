using System.Net;
using System.Net.Http.Json;
using NUnit.Framework;
using osuRequestor.Tests.Infrastructure.Harness;

namespace osuRequestor.Tests.Infrastructure.ExceptionHandling;

[TestFixture]
[Category("Integration")]
public class ApiExceptionPipelineTests : IntegrationTestBase
{
    [Test]
    public async Task ApiException_IsMappedToStatusAndResponseMessage()
    {
        var response = await this.Client.GetAsync("/api/request");
        var body     = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(body,                Does.Contain("Request player ID is null"));
        Assert.That(body,                Does.Not.Contain("Exception"));
        Assert.That(body,                Does.Not.Contain("   at "));
    }

    [Test]
    public async Task ApiException_UnknownBotClient_ReturnsUnauthorizedWithoutExceptionDetails()
    {
        var response = await this.Client.PostAsJsonAsync("/api/bot/auth", new
        {
            name         = "UnknownBot",
            clientSecret = "whatever"
        });
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(body,                Does.Not.Contain("KeyNotFoundException"));
        Assert.That(body,                Does.Not.Contain("BotAuthController"));
        Assert.That(body,                Does.Not.Contain("   at "));
    }
}
