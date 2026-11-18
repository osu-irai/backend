using System.Net;
using irai.Tests.Infrastructure.Harness;
using NUnit.Framework;

namespace irai.Tests.Features.Users;

[TestFixture]
[Category("Integration")]
public class UserEndpointTests : IntegrationTestBase
{
    [Test]
    public async Task OwnSettings_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await this.Client.GetAsync("/users/own/settings");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task OwnTwitch_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await this.Client.GetAsync("/users/own/twitch");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task OwnUser_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await this.Client.GetAsync("/users/own");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
