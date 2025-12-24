using System.Net;
using NUnit.Framework;
using osuRequestor.Tests.Infrastructure.Harness;

namespace osuRequestor.Tests.Features.Users;

[TestFixture]
[Category("Integration")]
public class UserEndpointTests : IntegrationTestBase
{
    [Test]
    public async Task OwnSettings_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await this.Client.GetAsync("/api/users/own/settings");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task OwnTwitch_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await this.Client.GetAsync("/api/users/own/twitch");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task OwnUser_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await this.Client.GetAsync("/api/users/own");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
