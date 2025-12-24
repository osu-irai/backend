using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using osuRequestor.Features.Requests.Models;
using osuRequestor.Infrastructure.Persistence;
using osuRequestor.Infrastructure.Persistence.Models;
using osuRequestor.Tests.Infrastructure.Factories;
using osuRequestor.Tests.Infrastructure.Fixtures;
using osuRequestor.Tests.Infrastructure.Harness;

namespace osuRequestor.Tests.Features.Requests;

[TestFixture]
[Category("Integration")]
public class RequestPipelineTests : IntegrationTestBase
{
    private static Task SeedSourceDestinationAndBeatmap(DatabaseContext db)
    {
        var source      = TestDataBuilder.User(1, "source");
        var destination = TestDataBuilder.User(2, "destination");
        var set         = TestDataBuilder.BeatmapSet(10);
        var beatmap     = TestDataBuilder.Beatmap(100, set);

        db.Users.AddRange(source, destination);
        db.BeatmapSets.Add(set);
        db.Beatmaps.Add(beatmap);

        return Task.CompletedTask;
    }

    private async Task<int> SeedAsyncWithRequestId()
    {
        var id = 0;
        await SeedAsync(db =>
        {
            var target  = TestDataBuilder.User(1, "target");
            var set     = TestDataBuilder.BeatmapSet(10);
            var beatmap = TestDataBuilder.Beatmap(100, set);
            var request = TestDataBuilder.Request(target, null, beatmap);

            db.Users.Add(target);
            db.BeatmapSets.Add(set);
            db.Beatmaps.Add(beatmap);
            db.Requests.Add(request);

            return Task.CompletedTask;
        });

        using var scope    = this.Factory.Services.CreateScope();
        var       database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        id = (await database.Requests.SingleAsync()).Id;
        return id;
    }

    private async Task<List<RequestModel>> ReadRequestsAsync()
    {
        using var scope = this.Factory.Services.CreateScope();
        var       db    = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        return await db.Requests
                       .Include(r => r.RequestedFrom)
                       .Include(r => r.RequestedTo)
                       .Include(r => r.Beatmap)
                       .ToListAsync();
    }

    [Test]
    public async Task GetRequests_ForUnknownPlayer_ReturnsEmptyList()
    {
        var response = await this.Client.GetAsync("/api/request?playerId=0");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var requests = await response.Content.ReadFromJsonAsync<List<ReceivedRequestResponse>>();
        Assert.That(requests, Is.Empty);
    }

    [Test]
    public async Task GetRequests_ReturnsAtMostFifty()
    {
        await SeedAsync(db =>
        {
            var target = TestDataBuilder.User(2, "target");
            db.Users.Add(target);

            for (var i = 0; i < 55; i++)
            {
                var set     = TestDataBuilder.BeatmapSet(1000 + i);
                var beatmap = TestDataBuilder.Beatmap(2000    + i, set);
                var request = TestDataBuilder.Request(target, null, beatmap);
                request.Date = DateTime.UtcNow.AddMinutes(-i);

                db.BeatmapSets.Add(set);
                db.Beatmaps.Add(beatmap);
                db.Requests.Add(request);
            }

            return Task.CompletedTask;
        });

        var response = await this.Client.GetAsync("/api/request?playerId=2");
        response.EnsureSuccessStatusCode();
        var requests = await response.Content.ReadFromJsonAsync<List<ReceivedRequestResponse>>();

        Assert.That(requests, Has.Count.EqualTo(50));
    }

    [Test]
    public async Task GetRequests_ReturnsOnlyNonDeletedRequestsNewestFirst()
    {
        await SeedAsync(db =>
        {
            var target = TestDataBuilder.User(2, "target");
            var source = TestDataBuilder.User(1, "source");
            db.Users.AddRange(target, source);

            for (var i = 0; i < 3; i++)
            {
                var set     = TestDataBuilder.BeatmapSet(10 + i);
                var beatmap = TestDataBuilder.Beatmap(100   + i, set);
                var request = TestDataBuilder.Request(target, source, beatmap);
                request.Date      = new DateTime(2026, 1, 1 + i, 0, 0, 0, DateTimeKind.Utc);
                request.IsDeleted = i == 2;

                db.BeatmapSets.Add(set);
                db.Beatmaps.Add(beatmap);
                db.Requests.Add(request);
            }

            return Task.CompletedTask;
        });

        var response = await this.Client.GetAsync("/api/request?playerId=2");
        response.EnsureSuccessStatusCode();
        var requests = await response.Content.ReadFromJsonAsync<List<ReceivedRequestResponse>>();

        Assert.That(requests,                                   Has.Count.EqualTo(2));
        Assert.That(requests!.Select(r => r.Beatmap.BeatmapId), Is.EqualTo(new[] { 101, 100 }));
    }

    [Test]
    public async Task GetRequests_WithoutPlayerId_ReturnsBadRequestWithApiMessage()
    {
        var response = await this.Client.GetAsync("/api/request");
        var body     = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(body,                Does.Contain("Request player ID is null"));
    }

    [Test]
    public async Task OwnRequestsDelete_WithInternalCookie_MarksRequestDeleted()
    {
        var requestId = await SeedAsyncWithRequestId();

        using var client = AuthenticatedTestClient.CreateInternalCookiesClient(this.Factory, 1);

        var response = await client.DeleteAsync($"/api/requests/own?requestId={requestId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var scope  = this.Factory.Services.CreateScope();
        var       db     = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var       stored = await db.Requests.SingleAsync(r => r.Id == requestId);
        Assert.That(stored.IsDeleted, Is.True);
    }

    [Test]
    public async Task OwnRequestsDelete_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await this.Client.DeleteAsync("/api/requests/own?requestId=1");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task OwnRequestsDelete_WithoutRequestId_ReturnsBadRequest()
    {
        using var client = AuthenticatedTestClient.CreateInternalCookiesClient(this.Factory, 1);

        var response = await client.DeleteAsync("/api/requests/own");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task OwnRequestsGet_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await this.Client.GetAsync("/api/requests/own");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task OwnRequestsPost_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await this.Client.PostAsJsonAsync("/api/requests/own", new
        {
            destinationName = "target",
            beatmapId       = 100
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task PostRequest_WithInvalidToken_ReturnsUnauthorized()
    {
        using var client = AuthenticatedTestClient.CreateClientWithToken(this.Factory, "not-a-jwt");

        var response = await client.PostAsJsonAsync("/api/request", new
        {
            destinationId = 2,
            beatmapId     = 100
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task PostRequest_WithMissingBeatmapAndNoOsuToken_ReturnsUnauthorized()
    {
        // RequestService falls back to the osu! API for an unknown beatmap, and the
        // per-user token provider requires a stored TokenModel. With none seeded the
        // action returns 401 instead of the desired 404.
        await SeedAsync(db =>
        {
            db.Users.AddRange(
                TestDataBuilder.User(1, "source"),
                TestDataBuilder.User(2, "destination"));

            return Task.CompletedTask;
        });

        using var client = AuthenticatedTestClient.CreateJwtClient(this.Factory, 1);

        var response = await client.PostAsJsonAsync("/api/request", new
        {
            destinationId = 2,
            beatmapId     = 999
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task PostRequest_WithMissingDestination_ReturnsNotFound()
    {
        await SeedAsync(db =>
        {
            var source  = TestDataBuilder.User(1, "source");
            var set     = TestDataBuilder.BeatmapSet(10);
            var beatmap = TestDataBuilder.Beatmap(100, set);

            db.Users.Add(source);
            db.BeatmapSets.Add(set);
            db.Beatmaps.Add(beatmap);

            return Task.CompletedTask;
        });

        using var client = AuthenticatedTestClient.CreateJwtClient(this.Factory, 1);

        var response = await client.PostAsJsonAsync("/api/request", new
        {
            destinationId = 999,
            beatmapId     = 100
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task PostRequest_WithProxyRoleWithoutSourceId_ReturnsBadRequest()
    {
        await SeedAsync(SeedSourceDestinationAndBeatmap);

        using var client = AuthenticatedTestClient.CreateJwtClient(this.Factory, 1, "Proxy");

        var response = await client.PostAsJsonAsync("/api/request", new
        {
            destinationId = 2,
            beatmapId     = 100
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task PostRequest_WithRolelessIrcToken_ReturnsForbidden()
    {
        using var client = await AuthenticatedTestClient.CreateIrcClientAsync(this.Factory);

        var response = await client.PostAsJsonAsync("/api/request", new
        {
            destinationId = 2,
            beatmapId     = 100
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task PostRequest_WithUserToken_CreatesWebsiteRequest()
    {
        await SeedAsync(SeedSourceDestinationAndBeatmap);

        using var client = AuthenticatedTestClient.CreateJwtClient(this.Factory, 1);

        var response = await client.PostAsJsonAsync("/api/request", new
        {
            destinationId = 2,
            beatmapId     = 100
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var stored = await ReadRequestsAsync();
        Assert.That(stored,                      Has.Count.EqualTo(1));
        Assert.That(stored[0].Source,            Is.EqualTo(RequestSource.Website));
        Assert.That(stored[0].RequestedFrom!.Id, Is.EqualTo(1));
        Assert.That(stored[0].RequestedTo.Id,    Is.EqualTo(2));
    }

    [Test]
    public async Task PostRequest_WithoutToken_ReturnsUnauthorized()
    {
        var response = await this.Client.PostAsJsonAsync("/api/request", new
        {
            destinationId = 2,
            beatmapId     = 100
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task SearchBeatmaps_WithoutInternalCookie_ReturnsUnauthorized()
    {
        var response = await this.Client.GetAsync("/api/search/beatmap?query=test");

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

        var response = await this.Client.GetAsync("/api/search/player?query=AL");
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

        var response = await this.Client.GetAsync("/api/search/player?query=");
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

        var response = await this.Client.GetAsync("/api/search/player");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SearchUserResponse>();

        Assert.That(result!.Count, Is.EqualTo(10));
        Assert.That(result.Players.Select(p => p.Username),
                    Is.EqualTo(Enumerable.Range(0, 10)
                                         .Select(i => $"player{i:00}")));
    }
}
