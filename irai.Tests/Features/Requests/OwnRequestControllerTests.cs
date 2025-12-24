using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using osu.NET;
using osu.NET.Authorization;
using osuRequestor.Common.Exceptions;
using osuRequestor.Features.Requests.Controllers;
using osuRequestor.Features.Requests.Models;
using osuRequestor.Features.Requests.Services;
using osuRequestor.Features.Requests.Validators;
using osuRequestor.Infrastructure.ExternalServices.OsuApi.Models;
using osuRequestor.Infrastructure.Messaging;
using osuRequestor.Infrastructure.Notifications;
using osuRequestor.Infrastructure.Persistence;
using osuRequestor.Infrastructure.Persistence.Models;
using osuRequestor.Tests.Common;

namespace osuRequestor.Tests.Features.Requests;

[TestFixture]
public class OwnRequestControllerTests
{
    private static (DatabaseContext Db, OwnRequestController Controller) CreateController()
    {
        var db = CreateDatabase();
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

        var controller = new OwnRequestController(
            NullLogger<OwnRequestController>.Instance,
            requestService);

        return (db, controller);
    }

    private static void SetUnauthenticatedContext(ControllerBase controller)
    {
        var authentication = Substitute.For<IAuthenticationService>();
        authentication
            .AuthenticateAsync(Arg.Any<HttpContext>(), Arg.Any<string>())
            .Returns(AuthenticateResult.NoResult());

        var services = new ServiceCollection()
                       .AddSingleton(authentication)
                       .BuildServiceProvider();

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = services }
        };
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

    private static RequestModel NewRequest(UserModel to, UserModel? from, BeatmapModel beatmap)
    {
        return new RequestModel
        {
            Beatmap       = beatmap,
            RequestedTo   = to,
            RequestedFrom = from,
            Source        = RequestSource.Website,
            Date          = DateTime.UtcNow
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
    /// We never do true deletions on requests
    /// </summary>
    [Test]
    public async Task DeleteSelfRequest_WhenRequestExists_SoftDeletesIt()
    {
        var (db, controller) = CreateController();
        await using var _db     = db;
        var             alice   = AddUser(db, 1, "alice");
        var             bob     = AddUser(db, 2, "bob");
        var             beatmap = AddBeatmap(db);
        var             request = NewRequest(alice, bob, beatmap);
        db.Requests.Add(request);
        await db.SaveChangesAsync();
        TestControllerContext.SignIn(controller, 1);

        var result = await controller.DeleteSelfRequest(request.Id);

        var stored = await db.Requests.SingleAsync();
        Assert.That(result,           Is.InstanceOf<OkResult>());
        Assert.That(stored.IsDeleted, Is.True);
    }

    /// <summary>
    /// Throw on unauthorized user
    /// </summary>
    [Test]
    public void GetSelfRequests_WhenNotAuthenticated_ThrowsUnauthorized()
    {
        var (db, controller) = CreateController();
        using var _db = db;
        SetUnauthenticatedContext(controller);

        Assert.ThrowsAsync<UnauthorizedException>(() => controller.GetSelfRequests());
    }

    /// <summary>
    /// Happy path
    /// </summary>
    [Test]
    public async Task GetSelfRequests_WhenSignedIn_ReturnsOwnRequests()
    {
        var (db, controller) = CreateController();
        await using var _db     = db;
        var             alice   = AddUser(db, 1, "alice");
        var             bob     = AddUser(db, 2, "bob");
        var             beatmap = AddBeatmap(db);
        db.Requests.AddRange(
            NewRequest(alice, bob,   beatmap),
            NewRequest(bob,   alice, beatmap));
        await db.SaveChangesAsync();
        TestControllerContext.SignIn(controller, 1);

        var result = await controller.GetSelfRequests();

        var ok       = result.Result as OkObjectResult;
        var requests = ok?.Value as List<ReceivedRequestResponse>;
        Assert.That(ok,                    Is.Not.Null);
        Assert.That(requests,              Has.Count.EqualTo(1));
        Assert.That(requests![0].From!.Id, Is.EqualTo(2));
    }

    /// <summary>
    /// Missing beatmap IDs fail validation before reaching the action
    /// </summary>
    [Test]
    public void PostRequestValidator_WhenBeatmapIsMissing_FailsValidation()
    {
        var result = new PostRequestWithNameValidator().Validate(new PostRequestWithName
        {
            DestinationName = "bob",
            BeatmapId       = null
        });

        Assert.That(result.IsValid, Is.False);
    }

    /// <summary>
    /// Missing destination names fail validation before reaching the action
    /// </summary>
    [Test]
    public void PostRequestValidator_WhenDestinationIsMissing_FailsValidation()
    {
        var result = new PostRequestWithNameValidator().Validate(new PostRequestWithName
        {
            DestinationName = null,
            BeatmapId       = 100
        });

        Assert.That(result.IsValid, Is.False);
    }

    /// <summary>
    /// Complete requests pass validation
    /// </summary>
    [Test]
    public void PostRequestValidator_WhenValid_PassesValidation()
    {
        var result = new PostRequestWithNameValidator().Validate(new PostRequestWithName
        {
            DestinationName = "bob",
            BeatmapId       = 100
        });

        Assert.That(result.IsValid, Is.True);
    }

    /// <summary>
    /// Happy path
    /// </summary>
    [Test]
    public async Task PostRequest_WhenValid_PersistsRequestForCurrentUser()
    {
        var (db, controller) = CreateController();
        await using var _db = db;
        AddUser(db, 1, "alice");
        AddUser(db, 2, "bob");
        AddBeatmap(db);
        await db.SaveChangesAsync();
        TestControllerContext.SignIn(controller, 1);

        var result = await controller.PostRequest(new PostRequestWithName
        {
            DestinationName = "bob",
            BeatmapId       = 100
        });

        var stored = await db.Requests.Include(requestModel => requestModel.RequestedFrom!)
                             .Include(requestModel => requestModel.RequestedTo)
                             .SingleAsync();
        Assert.That(result,                   Is.InstanceOf<OkResult>());
        Assert.That(stored.RequestedFrom!.Id, Is.EqualTo(1));
        Assert.That(stored.RequestedTo.Id,    Is.EqualTo(2));
        Assert.That(stored.Source,            Is.EqualTo(RequestSource.Website));
    }
}
