using System.Net;
using System.Net.Http.Json;
using NUnit.Framework;
using osuRequestor.Tests.Infrastructure.Factories;
using osuRequestor.Tests.Infrastructure.Fixtures;
using osuRequestor.Tests.Infrastructure.Harness;

namespace osuRequestor.Tests.Features.Bots;

[TestFixture]
[Category("Integration")]
public class BotEndpointTests : IntegrationTestBase
{
    [Test]
    public async Task IrcNames_WithIrcToken_ReturnsEnabledUsers()
    {
        await SeedAsync(db =>
        {
            db.Users.AddRange(
                TestDataBuilder.User(1, "alice", true),
                TestDataBuilder.User(2, "bob",   false));

            return Task.CompletedTask;
        });

        using var client = await AuthenticatedTestClient.CreateIrcClientAsync(this.Factory);

        var names = await client.GetFromJsonAsync<List<string>>("/api/bot/irc");

        Assert.That(names, Is.EquivalentTo(new[] { "alice" }));
    }

    [Test]
    public async Task IrcNames_WithoutToken_ReturnsUnauthorized()
    {
        var response = await this.Client.GetAsync("/api/bot/irc");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
