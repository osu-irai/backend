using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using osu.NET;
using osu.NET.Authorization;
using osuRequestor.Common.Exceptions;
using osuRequestor.Features.Requests.Models;
using osuRequestor.Features.Requests.Services;
using osuRequestor.Infrastructure.ExternalServices.OsuApi.Models;
using osuRequestor.Infrastructure.Messaging;
using osuRequestor.Infrastructure.Messaging.Contracts;
using osuRequestor.Infrastructure.Notifications;
using osuRequestor.Infrastructure.Persistence;
using osuRequestor.Infrastructure.Persistence.Models;

namespace osuRequestor.Tests.Features.Requests;

[TestFixture]
public class RequestServiceTests
{
    private static (DatabaseContext Db, RequestService Service, IRequestNotificationService Notification,
        IPublishEndpoint Publish) CreateHarness()
    {
        return CreateHarness(CreateDatabase());
    }

    private static (DatabaseContext Db, RequestService Service, IRequestNotificationService Notification,
        IPublishEndpoint Publish) CreateHarness(DatabaseContext db)
    {
        var osuClient = new OsuApiClient(
            Substitute.For<IOsuAccessTokenProvider>(),
            NullLogger<OsuApiClient>.Instance);

        var publish = Substitute.For<IPublishEndpoint>();
        var messageBus = new MessageBusService(
            publish,
            Substitute.For<ISendEndpointProvider>(),
            NullLogger<MessageBusService>.Instance);

        var notification = Substitute.For<IRequestNotificationService>();
        var service = new RequestService(
            NullLogger<RequestService>.Instance,
            db,
            osuClient,
            notification,
            messageBus);

        return (db, service, notification, publish);
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

    private static DatabaseContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
                      .UseInMemoryDatabase(Guid.NewGuid()
                                               .ToString())
                      .Options;

        return new DatabaseContext(options);
    }

    [Test]
    public async Task CreateRequest_WhenDestinationIdIsMissing_ThrowsNotFound()
    {
        var (db, service, _, _) = CreateHarness();
        await using var _db = db;
        AddUser(db, 1, "alice");
        AddBeatmap(db);
        await db.SaveChangesAsync();

        Assert.ThrowsAsync<NotFoundException>(() => service.CreateRequest(1, 404, 100));
    }

    [Test]
    public void CreateRequest_WhenDestinationIsMissing_ThrowsNotFound()
    {
        using var db = CreateDatabase();
        var (_, service, _, _) = CreateHarness(db);

        Assert.ThrowsAsync<NotFoundException>(() =>
                                                  service.CreateRequest(null, "missing-user", 1));
    }

    [Test]
    public async Task CreateRequest_WhenDestinationNameIsMissing_ThrowsNotFound()
    {
        var (db, service, _, _) = CreateHarness();
        await using var _db = db;
        AddUser(db, 1, "alice");
        AddBeatmap(db);
        await db.SaveChangesAsync();

        Assert.ThrowsAsync<NotFoundException>(() => service.CreateRequest(1, "nobody", 100));
    }

    [Test]
    public async Task CreateRequest_WhenDuplicate_UpdatesDateWithoutAddingRowAndStillNotifies()
    {
        var (db, service, notification, _) = CreateHarness();
        await using var _db     = db;
        var             source  = AddUser(db, 1, "alice");
        var             target  = AddUser(db, 2, "bob");
        var             beatmap = AddBeatmap(db);
        var             oldDate = DateTime.UtcNow.AddHours(-1);
        db.Requests.Add(new RequestModel
        {
            Beatmap       = beatmap,
            RequestedFrom = source,
            RequestedTo   = target,
            Source        = RequestSource.Website,
            Date          = oldDate
        });
        await db.SaveChangesAsync();

        var result = await service.CreateRequest(1, 2, 100);

        var stored = await db.Requests.SingleAsync();
        Assert.That(result.IsOk, Is.True);
        Assert.That(stored.Date, Is.GreaterThan(oldDate));
        await notification.Received(1)
                          .NotifyUserAsync("bob", Arg.Any<ReceivedRequestResponse>());
    }

    [Test]
    public async Task CreateRequest_WhenSourceIsMissing_ThrowsNotFound()
    {
        var (db, service, _, _) = CreateHarness();
        await using var _db = db;
        AddUser(db, 2, "bob");
        AddBeatmap(db);
        await db.SaveChangesAsync();

        Assert.ThrowsAsync<NotFoundException>(() => service.CreateRequest(404, 2, 100));
    }

    [Test]
    public async Task CreateRequest_WithDestinationName_PersistsRequest()
    {
        var (db, service, _, _) = CreateHarness();
        await using var _db = db;
        AddUser(db, 1, "alice");
        AddUser(db, 2, "bob");
        AddBeatmap(db);
        await db.SaveChangesAsync();

        var result = await service.CreateRequest(1, "bob", 100);

        var stored = await db.Requests.Include(requestModel => requestModel.RequestedFrom!)
                             .Include(requestModel => requestModel.RequestedTo)
                             .SingleAsync();
        Assert.That(result.IsOk,              Is.True);
        Assert.That(stored.RequestedFrom!.Id, Is.EqualTo(1));
        Assert.That(stored.RequestedTo.Id,    Is.EqualTo(2));
    }

    [Test]
    public async Task CreateRequest_WithSourceAndStoredBeatmap_PersistsAndNotifies()
    {
        var (db, service, notification, publish) = CreateHarness();
        await using var _db = db;
        AddUser(db, 1, "alice");
        AddUser(db, 2, "bob");
        AddBeatmap(db);
        await db.SaveChangesAsync();

        var result = await service.CreateRequest(1, 2, 100);

        var stored = await db.Requests.Include(requestModel => requestModel.RequestedFrom!)
                             .Include(requestModel => requestModel.RequestedTo)
                             .SingleAsync();
        Assert.That(result.IsOk,              Is.True);
        Assert.That(stored.RequestedFrom!.Id, Is.EqualTo(1));
        Assert.That(stored.RequestedTo.Id,    Is.EqualTo(2));
        Assert.That(stored.Source,            Is.EqualTo(RequestSource.Website));
        await notification.Received(1)
                          .NotifyUserAsync("bob",
                                           Arg.Is<ReceivedRequestResponse>(r =>
                                                                               r.Beatmap.BeatmapId == 100 &&
                                                                               r.From != null && r.From.Id == 1 &&
                                                                               r.Source == RequestSource.Website));
        await publish.Received(1)
                     .Publish(
                         Arg.Is<RequestContract>(c => c.Target == "bob" && c.Request.Beatmap.BeatmapId == 100),
                         Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateRequest_WithoutSource_PersistsRequestWithNullSource()
    {
        var (db, service, notification, _) = CreateHarness();
        await using var _db = db;
        AddUser(db, 2, "bob");
        AddBeatmap(db);
        await db.SaveChangesAsync();

        var result = await service.CreateRequest(null, 2, 100);

        var stored = await db.Requests.Include(requestModel => requestModel.RequestedFrom)
                             .SingleAsync();
        Assert.That(result.IsOk,          Is.True);
        Assert.That(stored.RequestedFrom, Is.Null);
        await notification.Received(1)
                          .NotifyUserAsync("bob",
                                           Arg.Is<ReceivedRequestResponse>(r => r.From == null));
    }

    /// <summary>
    /// Lookup by username and ID are the same
    /// </summary>
    [Test]
    public async Task GetUser_WhenNameAndIdExists_ReturnsSameUser()
    {
        var (db, service, _, _) = CreateHarness();
        await using var _db = db;
        AddUser(db, 1, "alice");
        await db.SaveChangesAsync();

        var byName = await service.GetUser("alice");
        var byId   = await service.GetUser(1);

        Assert.That(byName.IsOk, Is.True);
        Assert.That(byId.IsOk,   Is.True);

        Assert.That(byName.AsT0, Is.EqualTo(byId.AsT0));
    }

    [Test]
    public async Task GetUser_WhenNameExists_ReturnsUser()
    {
        var (db, service, _, _) = CreateHarness();
        await using var _db = db;
        AddUser(db, 1, "alice");
        await db.SaveChangesAsync();

        var result = await service.GetUser("alice");

        Assert.That(result.IsOk,    Is.True);
        Assert.That(result.AsT0.Id, Is.EqualTo(1));
    }

    [Test]
    public async Task GetUser_WhenNameIsMissing_ReturnsUserNotFound()
    {
        var (db, service, _, _) = CreateHarness();
        await using var _db = db;

        var result = await service.GetUser("nobody");

        Assert.That(result.IsT2,    Is.True);
        Assert.That(result.AsT2.Id, Is.Null);
    }

    [Test]
    public async Task GetUser_WhenUserExists_ReturnsUser()
    {
        var (db, service, _, _) = CreateHarness();
        await using var _db = db;
        AddUser(db, 7, "alice");
        await db.SaveChangesAsync();

        var result = await service.GetUser(7);

        Assert.That(result.IsOk,          Is.True);
        Assert.That(result.AsT0.Username, Is.EqualTo("alice"));
    }

    [Test]
    public async Task GetUser_WhenUserIsMissing_ReturnsUserNotFound()
    {
        var (db, service, _, _) = CreateHarness();
        await using var _db = db;

        var result = await service.GetUser(404);

        Assert.That(result.IsT2,    Is.True);
        Assert.That(result.AsT2.Id, Is.EqualTo(404));
    }

    [Test]
    public async Task GetRequestsToUser_WhenRequestIsDeleted_ExcludesIt()
    {
        var (db, service, _, _) = CreateHarness();
        await using var _db = db;
        var target = AddUser(db, 2, "bob");
        var source = AddUser(db, 1, "alice");
        var beatmap = AddBeatmap(db);
        db.Requests.Add(new RequestModel
        {
            Beatmap       = beatmap,
            RequestedTo   = target,
            RequestedFrom = source,
            Source        = RequestSource.Website,
            Date          = DateTime.UtcNow,
            IsDeleted     = true
        });
        await db.SaveChangesAsync();

        var result = await service.GetRequestsToUser(2);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetRequestsToUser_ReturnsNewestFirst()
    {
        var (db, service, _, _) = CreateHarness();
        await using var _db = db;
        var target = AddUser(db, 2, "bob");
        var source = AddUser(db, 1, "alice");
        var beatmap = AddBeatmap(db);
        var older = new RequestModel
        {
            Beatmap       = beatmap,
            RequestedTo   = target,
            RequestedFrom = source,
            Source        = RequestSource.Website,
            Date          = DateTime.UtcNow.AddHours(-1)
        };
        var newer = new RequestModel
        {
            Beatmap       = beatmap,
            RequestedTo   = target,
            RequestedFrom = source,
            Source        = RequestSource.Website,
            Date          = DateTime.UtcNow
        };
        db.Requests.AddRange(older, newer);
        await db.SaveChangesAsync();

        var result = await service.GetRequestsToUser(2);

        Assert.That(result.Select(r => r.Id), Is.EqualTo(new[] { newer.Id, older.Id }));
    }

    [Test]
    public async Task GetRequestsToUser_WithNoSource_ReturnsNullFrom()
    {
        var (db, service, _, _) = CreateHarness();
        await using var _db = db;
        var target = AddUser(db, 2, "bob");
        var beatmap = AddBeatmap(db);
        db.Requests.Add(new RequestModel
        {
            Beatmap       = beatmap,
            RequestedTo   = target,
            RequestedFrom = null,
            Source        = RequestSource.Twitch,
            Date          = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await service.GetRequestsToUser(2);

        Assert.That(result.Single().From, Is.Null);
    }

    [Test]
    public async Task GetRequestsToUser_ReturnsAtMostFifty()
    {
        var (db, service, _, _) = CreateHarness();
        await using var _db = db;
        var target = AddUser(db, 2, "bob");
        var beatmap = AddBeatmap(db);
        for (var i = 0; i < 55; i++)
        {
            db.Requests.Add(new RequestModel
            {
                Beatmap       = beatmap,
                RequestedTo   = target,
                RequestedFrom = null,
                Source        = RequestSource.Twitch,
                Date          = DateTime.UtcNow.AddSeconds(i)
            });
        }

        await db.SaveChangesAsync();

        var result = await service.GetRequestsToUser(2);

        Assert.That(result, Has.Count.EqualTo(50));
    }

    [Test]
    public async Task DeleteRequest_WhenExists_SoftDeletes()
    {
        var (db, service, _, _) = CreateHarness();
        await using var _db = db;
        var target = AddUser(db, 2, "bob");
        var beatmap = AddBeatmap(db);
        var request = new RequestModel
        {
            Beatmap       = beatmap,
            RequestedTo   = target,
            RequestedFrom = null,
            Source        = RequestSource.Twitch,
            Date          = DateTime.UtcNow
        };
        db.Requests.Add(request);
        await db.SaveChangesAsync();

        await service.DeleteRequest(request.Id);

        var stored = await db.Requests.SingleAsync();
        Assert.That(stored.IsDeleted, Is.True);
    }

    [Test]
    public async Task DeleteRequest_WhenMissing_DoesNotThrow()
    {
        var (db, service, _, _) = CreateHarness();
        await using var _db = db;

        Assert.DoesNotThrowAsync(() => service.DeleteRequest(404));
    }
}
