using System.Net;
using irai.Tests.Infrastructure.Factories;
using irai.Tests.Infrastructure.Fixtures;
using irai.Tests.Infrastructure.Harness;
using NUnit.Framework;

namespace irai.Tests.Features.Bots;

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
                TestDataBuilder.User(2, "bob"));

            return Task.CompletedTask;
        });

        using var client = await AuthenticatedTestClient.CreateIrcClientAsync(this.Factory);

        var names = await client.GetFromJsonAsync<List<string>>("/bot/irc");

        Assert.That(names, Is.EquivalentTo(new[] { "alice" }));
    }

    [Test]
    public async Task IrcNames_WithoutToken_ReturnsUnauthorized()
    {
        var response = await this.Client.GetAsync("/bot/irc");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
