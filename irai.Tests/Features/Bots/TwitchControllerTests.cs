using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using osu.NET;
using osu.NET.Authorization;
using osuRequestor.Features.Bots.Controllers;
using osuRequestor.Features.Bots.Services;
using osuRequestor.Features.Bots.Validators;
using osuRequestor.Features.Requests.Controllers;
using osuRequestor.Features.Requests.Models;
using osuRequestor.Features.Requests.Services;
using osuRequestor.Infrastructure.ExternalServices.OsuApi.Models;
using osuRequestor.Infrastructure.Messaging;
using osuRequestor.Infrastructure.Notifications;
using osuRequestor.Infrastructure.Persistence;
using osuRequestor.Infrastructure.Persistence.Models;

namespace osuRequestor.Tests.Features.Bots;

[TestFixture]
public class TwitchControllerTests
{
    private static TwitchController CreateController(DatabaseContext db)
    {
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
        var botService = new BotService(db);

        return new TwitchController(
            requestService,
            osuClient,
            NullLogger<OwnRequestController>.Instance,
            Substitute.For<IRequestNotificationService>(),
            botService);
    }

    private static void AddUserWithTwitch(DatabaseContext db, int id, string username, int twitchId,
                                          SettingsModel? settings)
    {
        var user = AddUser(db, id, username, settings);
        user.TwitchSettings = new TwitchModel
        {
            UserId       = id,
            TwitchId     = twitchId,
            Username     = $"{username}_tv",
            AccessToken  = "access",
            RefreshToken = "refresh"
        };
    }

    private static UserModel AddUser(DatabaseContext db, int id, string username, SettingsModel? settings = null)
    {
        var user = new UserModel
        {
            Id          = id,
            Username    = username,
            AvatarUrl   = $"https://a.ppy.sh/{id}",
            CountryCode = "US",
            Settings    = settings
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

    /// <summary>
    /// Drop users with disabled twitch integration
    /// </summary>
    [Test]
    public async Task GetAuthenticatedUsers_ReturnsOnlyEnabledTwitchUsersWithSettings()
    {
        var             db  = CreateDatabase();
        await using var _db = db;
        AddUserWithTwitch(db, 1, "enabled",     11, new SettingsModel { EnableTwitch = true });
        AddUserWithTwitch(db, 2, "disabled",    22, new SettingsModel { EnableTwitch = false });
        AddUserWithTwitch(db, 3, "no-settings", 33, null);
        await db.SaveChangesAsync();

        var controller = CreateController(db);

        var users = await controller.GetAuthenticatedUsers();

        Assert.That(users,             Has.Count.EqualTo(1));
        Assert.That(users[0].TwitchId, Is.EqualTo("11"));
        Assert.That(users[0].OsuId,    Is.EqualTo(1));
    }

    /// <summary>
    /// Happy path
    /// </summary>
    [Test]
    public async Task PostRequest_WhenValid_PersistsTwitchSourceRequest()
    {
        var             db  = CreateDatabase();
        await using var _db = db;
        AddUser(db, 2, "bob");
        AddBeatmap(db);
        await db.SaveChangesAsync();
        var controller = CreateController(db);

        var result = await controller.PostRequest(new PostBaseRequest
        {
            DestinationId = 2,
            BeatmapId     = 100
        });

        var stored = await db.Requests.Include(requestModel => requestModel.RequestedFrom)
                             .Include(requestModel => requestModel.RequestedTo)
                             .Include(requestModel => requestModel.Beatmap)
                             .SingleAsync();
        Assert.That(result,                Is.InstanceOf<OkResult>());
        Assert.That(stored.Source,         Is.EqualTo(RequestSource.Twitch));
        Assert.That(stored.RequestedFrom,  Is.Null);
        Assert.That(stored.RequestedTo.Id, Is.EqualTo(2));
        Assert.That(stored.Beatmap.Id,     Is.EqualTo(100));
    }
}
