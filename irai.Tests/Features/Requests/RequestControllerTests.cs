using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using osu.NET;
using osu.NET.Authorization;
using osuRequestor.Common.Exceptions;
using osuRequestor.Features.Requests.Controllers;
using osuRequestor.Features.Requests.Models;
using osuRequestor.Features.Requests.Services;
using osuRequestor.Infrastructure.ExternalServices.OsuApi.Models;
using osuRequestor.Infrastructure.Messaging;
using osuRequestor.Infrastructure.Notifications;
using osuRequestor.Infrastructure.Persistence;
using osuRequestor.Infrastructure.Persistence.Models;

namespace osuRequestor.Tests.Features.Requests;

[TestFixture]
public class RequestControllerTests
{
    private static (DatabaseContext Db, RequestController Controller) CreateController(int? currentUserId = null)
    {
        var db          = CreateDatabase();
        var userContext = Substitute.For<IUserContext>();
        userContext.GetCurrentUserId(Arg.Any<PostRequestRequest>())
                   .Returns(currentUserId);

        var osuClient = new OsuApiClient(
            Substitute.For<IOsuAccessTokenProvider>(),
            NullLogger<OsuApiClient>.Instance);
        var messageBus = new MessageBusService(
            Substitute.For<IPublishEndpoint>(),
            Substitute.For<ISendEndpointProvider>(),
            NullLogger<MessageBusService>.Instance);
        var requestService = new RequestService(
            NullLogger<RequestService>.Instance,
            db,
            osuClient,
            Substitute.For<IRequestNotificationService>(),
            messageBus);

        var controller = new RequestController(
            NullLogger<RequestController>.Instance,
            userContext,
            requestService);

        return (db, controller);
    }

    private static UserModel AddUser(DatabaseContext db, int id, string username)
    {
        var user = new UserModel
        {
            Id          = id,
            Username    = username,
            AvatarUrl   = $"https://a.ppy.sh/{id}",
            CountryCode = "US"
        };
        db.Users.Add(user);
        return user;
    }

    private static BeatmapModel AddBeatmap(DatabaseContext db, int id = 100)
    {
        var set = new BeatmapSetModel
        {
            Id        = id / 10,
            Artist    = "Test Artist",
            Title     = "Test Song",
            CreatorId = 1
        };
        var beatmap = new BeatmapModel
        {
            Id         = id,
            BeatmapSet = set,
            Version    = "Insane",
            Status     = BeatmapStatus.Ranked,
            Mode       = Mode.Osu
        };
        db.BeatmapSets.Add(set);
        db.Beatmaps.Add(beatmap);
        return beatmap;
    }

    private static RequestModel NewRequest(UserModel to, UserModel? from, BeatmapModel beatmap, bool deleted = false)
    {
        return new RequestModel
        {
            Beatmap       = beatmap,
            RequestedTo   = to,
            RequestedFrom = from,
            Source        = RequestSource.Website,
            Date          = DateTime.UtcNow,
            IsDeleted     = deleted
        };
    }

    private static DatabaseContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
                      .UseInMemoryDatabase(Guid.NewGuid()
                                               .ToString())
                      .Options;

        return new DatabaseContext(options);
    }

    /// <summary>
    /// Filter out deleted requests
    /// </summary>
    [Test]
    public async Task GetRequests_ReturnsOnlyActiveRequestsForPlayer()
    {
        var (db, controller) = CreateController();
        await using var _db     = db;
        var             alice   = AddUser(db, 1, "alice");
        var             bob     = AddUser(db, 2, "bob");
        var             beatmap = AddBeatmap(db);
        db.Requests.AddRange(
            NewRequest(bob,   alice, beatmap),
            NewRequest(alice, bob,   beatmap),
            NewRequest(bob,   alice, beatmap, true));
        await db.SaveChangesAsync();

        var result = await controller.GetRequests(2);

        var ok       = result.Result as OkObjectResult;
        var requests = ok?.Value as List<ReceivedRequestResponse>;
        Assert.That(ok,                             Is.Not.Null);
        Assert.That(requests,                       Has.Count.EqualTo(1));
        Assert.That(requests![0].Beatmap.BeatmapId, Is.EqualTo(beatmap.Id));
    }

    /// <summary>
    /// Requests can be source-less in some cases
    /// </summary>
    [Test]
    public async Task PostRequest_WhenContextHasNoSource_CreatesRequestWithoutSource()
    {
        var (db, controller) = CreateController();
        await using var _db = db;
        AddUser(db, 2, "bob");
        AddBeatmap(db);
        await db.SaveChangesAsync();

        var result = await controller.PostRequest(new PostRequestRequest
        {
            DestinationId = 2,
            BeatmapId     = 100
        });

        var stored = await db.Requests.Include(requestModel => requestModel.RequestedFrom)
                             .SingleAsync();
        Assert.That(result,               Is.InstanceOf<OkResult>());
        Assert.That(stored.RequestedFrom, Is.Null);
    }

    /// <summary>
    /// Destination user does not exist
    /// </summary>
    [Test]
    public async Task PostRequest_WhenDestinationIsMissing_ThrowsNotFound()
    {
        var (db, controller) = CreateController(1);
        await using var _db = db;
        AddUser(db, 1, "alice");
        await db.SaveChangesAsync();

        Assert.ThrowsAsync<NotFoundException>(() => controller.PostRequest(new PostRequestRequest
        {
            DestinationId = 404,
            BeatmapId     = 100
        }));
    }

    /// <summary>
    /// Happy path
    /// </summary>
    [Test]
    public async Task PostRequest_WhenSourceAndDestinationExist_PersistsRequest()
    {
        var (db, controller) = CreateController(1);
        await using var _db = db;
        AddUser(db, 1, "alice");
        AddUser(db, 2, "bob");
        AddBeatmap(db);
        await db.SaveChangesAsync();

        var result = await controller.PostRequest(new PostRequestRequest
        {
            DestinationId = 2,
            BeatmapId     = 100
        });

        var stored = await db.Requests.Include(requestModel => requestModel.RequestedFrom!)
                             .Include(requestModel => requestModel.RequestedTo)
                             .SingleAsync();
        Assert.That(result,                   Is.InstanceOf<OkResult>());
        Assert.That(stored.RequestedFrom!.Id, Is.EqualTo(1));
        Assert.That(stored.RequestedTo.Id,    Is.EqualTo(2));
    }
}
