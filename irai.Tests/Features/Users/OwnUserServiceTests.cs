using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using osuRequestor.Features.Users.Models;
using osuRequestor.Features.Users.Services;
using osuRequestor.Infrastructure.ExternalServices.OsuApi.Models;
using osuRequestor.Infrastructure.Messaging;
using osuRequestor.Infrastructure.Notifications;
using osuRequestor.Infrastructure.Persistence;
using osuRequestor.Infrastructure.Persistence.Models;

namespace osuRequestor.Tests.Features.Users;

[TestFixture]
public class OwnUserServiceTests
{
    private static Harness CreateHarness()
    {
        var db           = CreateDatabase();
        var notification = Substitute.For<IRequestNotificationService>();
        var messageBus   = Substitute.For<IMessageBusService>();
        var service = new OwnUserService(
            db,
            NullLogger<OwnUserService>.Instance,
            notification,
            messageBus);

        return new Harness(db, service, notification, messageBus);
    }

    private static DatabaseContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
                      .UseInMemoryDatabase(Guid.NewGuid().ToString())
                      .Options;

        return new DatabaseContext(options);
    }

    private static UserModel AddUser(DatabaseContext db, int id, string username,
                                     SettingsModel? settings = null,
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

    private static RequestModel NewRequest(UserModel to, UserModel? from, BeatmapModel beatmap,
                                           bool deleted = false)
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

    [Test]
    public async Task GetSelf_ReturnsUserAndCountsOnlyActiveRequests()
    {
        var harness = CreateHarness();
        await using var db = harness.Db;
        var user = AddUser(db, 1, "alice");
        var source = AddUser(db, 2, "bob");
        var beatmap = AddBeatmap(db);
        db.Requests.Add(NewRequest(user, source, beatmap));
        db.Requests.Add(NewRequest(user, null, beatmap));
        db.Requests.Add(NewRequest(user, source, beatmap, deleted: true));
        await db.SaveChangesAsync();

        var result = await harness.Service.GetSelf(1);

        Assert.That(result.User.Id, Is.EqualTo(1));
        Assert.That(result.User.Username, Is.EqualTo("alice"));
        Assert.That(result.User.AvatarUrl, Is.EqualTo("https://a.ppy.sh/1"));
        Assert.That(result.RequestCount, Is.EqualTo(2));
    }

    [Test]
    public void GetSelf_WhenUserIsMissing_Throws()
    {
        var harness = CreateHarness();
        using var db = harness.Db;

        Assert.ThrowsAsync<NullReferenceException>(() => harness.Service.GetSelf(404));
    }

    [Test]
    public async Task GetSettings_WhenNoSettingsExist_ReturnsNull()
    {
        var harness = CreateHarness();
        await using var db = harness.Db;
        AddUser(db, 1, "alice");
        await db.SaveChangesAsync();

        var result = await harness.Service.GetSettings(1);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetSettings_WhenSettingsExist_ReturnsStoredValues()
    {
        var harness = CreateHarness();
        await using var db = harness.Db;
        AddUser(db, 1, "alice", new SettingsModel { EnableIrc = true, EnableTwitch = true });
        await db.SaveChangesAsync();

        var result = await harness.Service.GetSettings(1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.EnableIrc, Is.True);
        Assert.That(result.EnableTwitch, Is.True);
    }

    [Test]
    public async Task UpdateSettings_WhenSettingsExist_UpdatesAndNotifiesBothBots()
    {
        var harness = CreateHarness();
        await using var db = harness.Db;
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

        await harness.Service.UpdateSettings(1, new SettingsDTO
        {
            EnableIrc    = true,
            EnableTwitch = true
        });

        var stored = await db.Settings.SingleAsync();
        Assert.That(stored.EnableIrc, Is.True);
        Assert.That(stored.EnableTwitch, Is.True);
        await harness.Notification.Received(1).NotifyAboutIrcChange("alice", true);
        await harness.MessageBus.Received(1).SubmitIrcSettingsChangeAsync("alice", true);
        await harness.MessageBus.Received(1).SubmitTwitchSettingsChangeAsync("55", 1, true);
    }

    [Test]
    public async Task UpdateSettings_WhenNoSettingsExist_CreatesOnlyIrcSettingsWithoutSideEffects()
    {
        var harness = CreateHarness();
        await using var db = harness.Db;
        AddUser(db, 1, "alice");
        await db.SaveChangesAsync();

        await harness.Service.UpdateSettings(1, new SettingsDTO
        {
            EnableIrc    = true,
            EnableTwitch = true
        });

        var stored = await db.Settings.SingleAsync();
        Assert.That(stored.UserId, Is.EqualTo(1));
        Assert.That(stored.EnableIrc, Is.True);
        Assert.That(stored.EnableTwitch, Is.False);
        await harness.Notification.DidNotReceiveWithAnyArgs().NotifyAboutIrcChange(default!, default);
        await harness.MessageBus.DidNotReceiveWithAnyArgs().SubmitIrcSettingsChangeAsync(default!, default);
        await harness.MessageBus.DidNotReceiveWithAnyArgs().SubmitTwitchSettingsChangeAsync(default!, default, default);
    }

    [Test]
    public async Task UpdateSettings_WhenNoTwitchLink_DoesNotSendTwitchChange()
    {
        var harness = CreateHarness();
        await using var db = harness.Db;
        AddUser(db, 1, "alice", new SettingsModel { EnableIrc = false, EnableTwitch = false });
        await db.SaveChangesAsync();

        await harness.Service.UpdateSettings(1, new SettingsDTO
        {
            EnableIrc    = true,
            EnableTwitch = true
        });

        await harness.MessageBus.DidNotReceiveWithAnyArgs()
                     .SubmitTwitchSettingsChangeAsync(default!, default, default);
    }

    [Test]
    public async Task GetTwitchUsername_WhenMissing_ReturnsNull()
    {
        var harness = CreateHarness();
        await using var db = harness.Db;
        AddUser(db, 1, "alice");
        await db.SaveChangesAsync();

        var result = await harness.Service.GetTwitchUsername(1);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetTwitchUsername_WhenPresent_ReturnsUsername()
    {
        var harness = CreateHarness();
        await using var db = harness.Db;
        AddUser(db, 1, "alice", twitch: new TwitchModel
        {
            UserId       = 1,
            TwitchId     = 55,
            Username     = "alice_tv",
            AccessToken  = "access",
            RefreshToken = "refresh"
        });
        await db.SaveChangesAsync();

        var result = await harness.Service.GetTwitchUsername(1);

        Assert.That(result, Is.EqualTo("alice_tv"));
    }

    private sealed record Harness(
        DatabaseContext Db,
        OwnUserService Service,
        IRequestNotificationService Notification,
        IMessageBusService MessageBus);
}
