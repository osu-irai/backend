using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using osuRequestor.Features.Bots.Models;
using osuRequestor.Infrastructure.Persistence;
using osuRequestor.Infrastructure.Persistence.Models;
using osuRequestor.Tests.Infrastructure.Factories;
using osuRequestor.Tests.Infrastructure.Fixtures;
using osuRequestor.Tests.Infrastructure.Harness;

namespace osuRequestor.Tests.Features.Bots;

[TestFixture]
[Category("Integration")]
public class TwitchBotEndpointTests : IntegrationTestBase
{
    private static Task SeedNormalIdsAsync(DatabaseContext db)
    {
        var destination = TestDataBuilder.User(2, "destination");
        var set         = TestDataBuilder.BeatmapSet(10);
        var beatmap     = TestDataBuilder.Beatmap(100, set);

        db.Users.Add(destination);
        db.BeatmapSets.Add(set);
        db.Beatmaps.Add(beatmap);

        return Task.CompletedTask;
    }

    private static TwitchModel Twitch(UserModel user, int twitchId)
    {
        return new TwitchModel
        {
            User         = user,
            UserId       = user.Id,
            TwitchId     = twitchId,
            Username     = $"twitch-{user.Username}",
            AccessToken  = "access",
            RefreshToken = "refresh"
        };
    }

    private async Task<List<RequestModel>> ReadRequestsAsync()
    {
        using var scope = this.Factory.Services.CreateScope();
        var       db    = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        return await db.Requests
                       .Include(request => request.RequestedTo)
                       .Include(request => request.RequestedFrom)
                       .Include(request => request.Beatmap)
                       .ToListAsync();
    }

    /// <summary>
    /// Controller permission mismatch
    /// </summary>
    [Test]
    public async Task IrcNames_WithTwitchToken_ReturnsForbidden()
    {
        using var client = await AuthenticatedTestClient.CreateTwitchClientAsync(this.Factory);

        var response = await client.GetAsync("/api/bot/irc");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    /// <summary>
    /// Controller permission mismatch
    /// </summary>
    [Test]
    public async Task TwitchInfo_WithIrcToken_ReturnsForbidden()
    {
        using var client = await AuthenticatedTestClient.CreateIrcClientAsync(this.Factory);

        var response = await client.GetAsync("/api/twitch/info");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    /// <summary>
    /// Disabled users filtered
    /// </summary>
    [Test]
    public async Task TwitchInfo_WithTwitchToken_ReturnsOnlyEnabledTwitchUsers()
    {
        await SeedAsync(db =>
        {
            var enabled  = TestDataBuilder.User(1, "enabled",  enableTwitch: true);
            var disabled = TestDataBuilder.User(2, "disabled", enableTwitch: false);
            db.Users.AddRange(enabled, disabled);
            db.Twitch.AddRange(
                Twitch(enabled,  111),
                Twitch(disabled, 222));

            return Task.CompletedTask;
        });

        using var client = await AuthenticatedTestClient.CreateTwitchClientAsync(this.Factory);

        var users = await client.GetFromJsonAsync<List<TwitchDTO>>("/api/twitch/info");

        Assert.That(users,             Has.Count.EqualTo(1));
        Assert.That(users![0].OsuId,   Is.EqualTo(1));
        Assert.That(users[0].TwitchId, Is.EqualTo("111"));
    }

    /// <summary>
    /// Auth failure
    /// </summary>
    [Test]
    public async Task TwitchInfo_WithoutToken_ReturnsUnauthorized()
    {
        var response = await this.Client.GetAsync("/api/twitch/info");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>
    /// Request deduplication for Twitch requests
    /// </summary>
    [Test]
    public async Task TwitchPost_DuplicateRequest_DoesNotCreateSecondRow()
    {
        await SeedAsync(SeedNormalIdsAsync);

        using var client  = await AuthenticatedTestClient.CreateTwitchClientAsync(this.Factory);
        var       payload = new { destinationId = 2, beatmapId = 100 };

        var first  = await client.PostAsJsonAsync("/api/twitch/info", payload);
        var second = await client.PostAsJsonAsync("/api/twitch/info", payload);

        Assert.That(first.StatusCode,  Is.EqualTo(HttpStatusCode.OK));
        Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var stored = await ReadRequestsAsync();

        Assert.That(stored,           Has.Count.EqualTo(1));
        Assert.That(stored[0].Source, Is.EqualTo(RequestSource.Twitch));
    }

    /// <summary>
    /// Standard bad request on partially defined inputs
    /// </summary>
    [Test]
    public async Task TwitchPost_WithMissingFields_ReturnsBadRequest()
    {
        using var client = await AuthenticatedTestClient.CreateTwitchClientAsync(this.Factory);

        var response = await client.PostAsJsonAsync("/api/twitch/info", new { });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>
    /// Posted requests are being stored correctly
    /// </summary>
    [Test]
    public async Task TwitchPost_WithNormalIds_PersistsCorrectTwitchSourcedRequest()
    {
        await SeedAsync(SeedNormalIdsAsync);

        using var client = await AuthenticatedTestClient.CreateTwitchClientAsync(this.Factory);

        var response = await client.PostAsJsonAsync("/api/twitch/info", new
        {
            destinationId = 2,
            beatmapId     = 100
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var stored = await ReadRequestsAsync();

        Assert.That(stored,                   Has.Count.EqualTo(1));
        Assert.That(stored[0].Source,         Is.EqualTo(RequestSource.Twitch));
        Assert.That(stored[0].RequestedTo.Id, Is.EqualTo(2));
        Assert.That(stored[0].Beatmap.Id,     Is.EqualTo(100));
        Assert.That(stored[0].RequestedFrom,  Is.Null);
    }

    /// <summary>
    /// Standard bad request on null
    /// </summary>
    [Test]
    public async Task TwitchPost_WithoutContent_ReturnsBadRequest()
    {
        using var client = await AuthenticatedTestClient.CreateTwitchClientAsync(this.Factory);

        var response = await client.PostAsJsonAsync<object>("/api/twitch/info", null!);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
