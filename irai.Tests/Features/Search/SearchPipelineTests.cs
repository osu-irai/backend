using System.Net;
using NUnit.Framework;
using osuRequestor.Features.Search.Models;
using osuRequestor.Tests.Infrastructure.Factories;
using osuRequestor.Tests.Infrastructure.Fixtures;
using osuRequestor.Tests.Infrastructure.Harness;

namespace osuRequestor.Tests.Features.Search;

[TestFixture]
[Category("Integration")]
public class SearchPipelineTests : IntegrationTestBase
{
    [Test]
    public async Task SearchBeatmaps_WithoutInternalCookie_ReturnsUnauthorized()
    {
        var response = await this.Client.GetAsync("/search/beatmap?query=test");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task SearchPlayers_IsCaseInsensitivePrefixMatch()
    {
        await SeedAsync(db =>
        {
            db.Users.AddRange(
                TestDataBuilder.User(1, "Alice"),
                TestDataBuilder.User(2, "alvin"),
                TestDataBuilder.User(3, "Bob"));

            return Task.CompletedTask;
        });

        using var client   = AuthenticatedTestClient.CreateInternalCookiesClient(this.Factory, 1);
        var       response = await client.GetAsync("/search/player?query=AL");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SearchUserResponse>();

        Assert.That(result!.Players.Select(p => p.Username), Is.EquivalentTo(new[] { "Alice", "alvin" }));
    }

    [Test]
    public async Task SearchPlayers_WithEmptyQuery_ReturnsAtMostTen()
    {
        await SeedAsync(db =>
        {
            for (var i = 0; i < 12; i++)
            {
                db.Users.Add(TestDataBuilder.User(i + 1, $"player{i:00}"));
            }

            return Task.CompletedTask;
        });

        using var client   = AuthenticatedTestClient.CreateInternalCookiesClient(this.Factory, 1);
        var       response = await client.GetAsync("/search/player?query=");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SearchUserResponse>();

        Assert.That(result!.Count, Is.EqualTo(10));
    }

    [Test]
    public async Task SearchPlayers_WithNullQuery_ReturnsAtMostTenOrdered()
    {
        await SeedAsync(db =>
        {
            for (var i = 0; i < 12; i++)
            {
                db.Users.Add(TestDataBuilder.User(i + 1, $"player{i:00}"));
            }

            return Task.CompletedTask;
        });

        using var client   = AuthenticatedTestClient.CreateInternalCookiesClient(this.Factory, 1);
        var       response = await client.GetAsync("/search/player");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SearchUserResponse>();

        Assert.That(result!.Count, Is.EqualTo(10));
        Assert.That(result.Players.Select(p => p.Username),
                    Is.EqualTo(Enumerable.Range(0, 10)
                                         .Select(i => $"player{i:00}")));
    }
}
