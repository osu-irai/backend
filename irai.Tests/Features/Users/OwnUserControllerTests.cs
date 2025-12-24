using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using osuRequestor.Features.Users.Controllers;
using osuRequestor.Features.Users.Models;
using osuRequestor.Features.Users.Services;
using osuRequestor.Infrastructure.Configuration;
using osuRequestor.Infrastructure.ExternalServices.OsuApi.Models;
using osuRequestor.Infrastructure.ExternalServices.TwitchApi;
using osuRequestor.Infrastructure.Messaging;
using osuRequestor.Infrastructure.Messaging.Contracts;
using osuRequestor.Infrastructure.Notifications;
using osuRequestor.Infrastructure.Persistence;
using osuRequestor.Infrastructure.Persistence.Models;
using osuRequestor.Tests.Common;

namespace osuRequestor.Tests.Features.Users;

[TestFixture]
public class OwnUserControllerTests
{
    private static Harness CreateHarness()
    {
        var db               = CreateDatabase();
        var notification     = Substitute.For<IRequestNotificationService>();
        var ircEndpoint      = Substitute.For<ISendEndpoint>();
        var twitchEndpoint   = Substitute.For<ISendEndpoint>();
        var endpointProvider = Substitute.For<ISendEndpointProvider>();
        endpointProvider.GetSendEndpoint(new Uri("queue:irc-settings"))
                        .Returns(ircEndpoint);
        endpointProvider.GetSendEndpoint(new Uri("queue:twitch-settings"))
                        .Returns(twitchEndpoint);

        var messageBus = new MessageBusService(
            Substitute.For<IPublishEndpoint>(),
            endpointProvider,
            NullLogger<MessageBusService>.Instance);

        var service = new OwnUserService(
            db,
            NullLogger<OwnUserService>.Instance,
            notification,
            messageBus);

        var controller = new OwnUserController(service);

        return new Harness(db, controller, notification, endpointProvider, ircEndpoint, twitchEndpoint);
    }

    private static UserModel AddUser(DatabaseContext db, int id, string username, SettingsModel? settings = null,
                                     TwitchModel? twitch = null)
    {
        var user = new UserModel
        {
            Id             = id,
            Username       = username,
            AvatarUrl      = $"https://a.ppy.sh/{id}",
            CountryCode    = "US",
            Settings       = settings,
            TwitchSettings = twitch
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

    private sealed record Harness(
        DatabaseContext Db,
        OwnUserController Controller,
        IRequestNotificationService Notification,
        ISendEndpointProvider EndpointProvider,
        ISendEndpoint IrcEndpoint,
        ISendEndpoint TwitchEndpoint);

    [Test]
    public async Task SelfGetSettings_WhenNoSettingsExist_ReturnsNull()
    {
        var             harness = CreateHarness();
        await using var db      = harness.Db;
        AddUser(db, 1, "alice");
        await db.SaveChangesAsync();
        TestControllerContext.SignIn(harness.Controller, 1);

        var result = await harness.Controller.SelfGetSettings();

        Assert.That(result?.Value, Is.Null);
    }

    [Test]
    public async Task SelfGetSettings_WhenSettingsExist_ReturnsStoredValues()
    {
        var             harness = CreateHarness();
        await using var db      = harness.Db;
        AddUser(db, 1, "alice", new SettingsModel { EnableIrc = true, EnableTwitch = true });
        await db.SaveChangesAsync();
        TestControllerContext.SignIn(harness.Controller, 1);

        var result = await harness.Controller.SelfGetSettings();

        Assert.That(result!.Value,             Is.Not.Null);
        Assert.That(result.Value!.EnableIrc,   Is.True);
        Assert.That(result.Value.EnableTwitch, Is.True);
    }

    [Test]
    public async Task SelfGetTwitchUsername_WhenMissing_ReturnsUnauthorized()
    {
        var             harness = CreateHarness();
        await using var db      = harness.Db;
        AddUser(db, 1, "alice");
        await db.SaveChangesAsync();
        TestControllerContext.SignIn(harness.Controller, 1);

        var result = await harness.Controller.SelfGetTwitchUsername();

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task SelfGetTwitchUsername_WhenPresent_ReturnsUsername()
    {
        var             harness = CreateHarness();
        await using var db      = harness.Db;
        AddUser(db, 1, "alice", twitch: new TwitchModel
        {
            UserId       = 1,
            TwitchId     = 55,
            Username     = "alice_tv",
            AccessToken  = "access",
            RefreshToken = "refresh"
        });
        await db.SaveChangesAsync();
        TestControllerContext.SignIn(harness.Controller, 1);

        var result = await harness.Controller.SelfGetTwitchUsername();

        var ok = result as OkObjectResult;
        Assert.That(ok,        Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo("alice_tv"));
    }

    [Test]
    public async Task SelfSetSettings_WhenNoSettingsExist_CreatesSettings()
    {
        var             harness = CreateHarness();
        await using var db      = harness.Db;
        AddUser(db, 1, "alice");
        await db.SaveChangesAsync();
        TestControllerContext.SignIn(harness.Controller, 1);

        var result = await harness.Controller.SelfSetSettings(new SettingsDTO
        {
            EnableIrc    = true,
            EnableTwitch = true
        });

        var stored = await db.Settings.SingleAsync();
        Assert.That(result,              Is.InstanceOf<OkResult>());
        Assert.That(stored.UserId,       Is.EqualTo(1));
        Assert.That(stored.EnableIrc,    Is.True);
        Assert.That(stored.EnableTwitch, Is.False);
        await harness.Notification.DidNotReceiveWithAnyArgs()
                     .NotifyAboutIrcChange(default!, default);
    }

    [Test]
    public async Task SelfSetSettings_WhenSettingsExist_UpdatesAndNotifies()
    {
        var             harness = CreateHarness();
        await using var db      = harness.Db;
        AddUser(db, 1, "alice",
                new SettingsModel { EnableIrc = false, EnableTwitch = false },
                new TwitchModel
                {
                    UserId       = 1,
                    TwitchId     = 55,
                    Username     = "alice_tv",
                    AccessToken  = "access",
                    RefreshToken = "refresh"
                });
        await db.SaveChangesAsync();
        TestControllerContext.SignIn(harness.Controller, 1);

        var result = await harness.Controller.SelfSetSettings(new SettingsDTO
        {
            EnableIrc    = true,
            EnableTwitch = true
        });

        var stored = await db.Settings.SingleAsync();
        Assert.That(result,              Is.InstanceOf<OkResult>());
        Assert.That(stored.EnableIrc,    Is.True);
        Assert.That(stored.EnableTwitch, Is.True);
        await harness.Notification.Received(1)
                     .NotifyAboutIrcChange("alice", true);
        await harness.EndpointProvider.Received(1)
                     .GetSendEndpoint(new Uri("queue:irc-settings"));
        await harness.IrcEndpoint.Received(1)
                     .Send(
                         Arg.Is<IrcSettingsChangeContract>(c => c.Username == "alice" && c.IsEnabled),
                         Arg.Any<CancellationToken>());
        await harness.EndpointProvider.Received(1)
                     .GetSendEndpoint(new Uri("queue:twitch-settings"));
        await harness.TwitchEndpoint.Received(1)
                     .Send(
                         Arg.Is<TwitchSettingsChangeContract>(c => c.TwitchUserId == "55" && c.OsuId == 1 &&
                                                                   c.IsEnabled),
                         Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SelfUserGet_ReturnsUserAndCountOfNonDeletedRequests()
    {
        var             harness = CreateHarness();
        await using var db      = harness.Db;
        var             alice   = AddUser(db, 1, "alice");
        var             bob     = AddUser(db, 2, "bob");
        var             beatmap = AddBeatmap(db);
        db.Requests.AddRange(
            NewRequest(alice, bob, beatmap),
            NewRequest(alice, bob, beatmap),
            NewRequest(alice, bob, beatmap, true));
        await db.SaveChangesAsync();
        TestControllerContext.SignIn(harness.Controller, 1);

        var result = await harness.Controller.SelfUserGet();

        Assert.That(result.Value,               Is.Not.Null);
        Assert.That(result.Value!.User.Id,      Is.EqualTo(1));
        Assert.That(result.Value.User.Username, Is.EqualTo("alice"));
        Assert.That(result.Value.RequestCount,  Is.EqualTo(2));
    }
}
