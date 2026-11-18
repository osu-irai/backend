using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using irai.Common.Constants;
using irai.Features.Authentication.Persistence;
using irai.Features.Authentication.Services;
using irai.Features.Users.Persistence;
using irai.Infrastructure.ExternalServices.OsuApi.Models;
using irai.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace irai.Tests.Features.Authentication;

[TestFixture]
public class AuthenticationServiceTests
{
    private static readonly IDataProtectionProvider Protection = DataProtectionProvider.Create("irai-tests");
    private static readonly IDataProtector Protector = Protection.CreateProtector(TokenProtection.PURPOSE);

    private IBackgroundJobClient _backgroundJobClient;
    private IFriendService _friendService;

    private static DatabaseContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
                      .UseInMemoryDatabase(Guid.NewGuid()
                                               .ToString())
                      .Options;

        return new DatabaseContext(options);
    }

    private AuthenticationService CreateService(DatabaseContext db)
    {
        this._backgroundJobClient = Substitute.For<IBackgroundJobClient>();
        this._friendService       = Substitute.For<IFriendService>();
        return new AuthenticationService(this._backgroundJobClient, this._friendService, db, Protection,
                                         NullLogger<AuthenticationService>.Instance);
    }

    private static UserModel AddUser(DatabaseContext db, int id, string username,
                                     SettingsModel? settings = null)
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

    [Test]
    public async Task SaveOsuLogin_WhenUserExists_UpdatesProfileAndExistingToken()
    {
        await using var db   = CreateDatabase();
        var             user = AddUser(db, 1, "old-name", new SettingsModel());
        db.Tokens.Add(new TokenModel
        {
            UserId       = 1,
            AccessToken  = "old-access",
            RefreshToken = "old-refresh",
            Expires      = DateTime.UtcNow.AddMinutes(-1)
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.SaveOsuLogin(new User
        {
            Id          = 1,
            Username    = "new-name",
            CountryCode = "DE",
            AvatarUrl   = user.AvatarUrl
        }, "new-access", "new-refresh");

        var storedUser  = await db.Users.SingleAsync();
        var storedToken = await db.Tokens.SingleAsync();
        Assert.That(storedUser.Username,                           Is.EqualTo("new-name"));
        Assert.That(storedUser.CountryCode,                        Is.EqualTo("DE"));
        Assert.That(Protector.Unprotect(storedToken.AccessToken),  Is.EqualTo("new-access"));
        Assert.That(Protector.Unprotect(storedToken.RefreshToken), Is.EqualTo("new-refresh"));
        Assert.That(await db.Settings.CountAsync(),                Is.EqualTo(1));
        this._backgroundJobClient.Received(0)
            .Create(Arg.Is<Job>(j => j.Method.Name == nameof(IFriendService.GetFriends)), Arg.Any<EnqueuedState>());
    }

    [Test]
    public async Task SaveOsuLogin_WhenUserIsNew_CreatesUserSettingsAndToken()
    {
        await using var db      = CreateDatabase();
        var             service = CreateService(db);
        var             before  = DateTime.UtcNow;

        var expiration = await service.SaveOsuLogin(new User
        {
            Id          = 1,
            Username    = "alice",
            CountryCode = "PL",
            AvatarUrl   = "https://a.ppy.sh/1"
        }, "access", "refresh");

        var user = await db.Users.Include(u => u.Settings)
                           .SingleAsync();
        var token = await db.Tokens.SingleAsync();
        Assert.That(user.Id,                                 Is.EqualTo(1));
        Assert.That(user.Username,                           Is.EqualTo("alice"));
        Assert.That(user.CountryCode,                        Is.EqualTo("PL"));
        Assert.That(user.Settings,                           Is.Not.Null);
        Assert.That(token.UserId,                            Is.EqualTo(1));
        Assert.That(Protector.Unprotect(token.AccessToken),  Is.EqualTo("access"));
        Assert.That(Protector.Unprotect(token.RefreshToken), Is.EqualTo("refresh"));
        Assert.That(expiration,                              Is.GreaterThanOrEqualTo(before.AddDays(1)));
        Assert.That(expiration,                              Is.LessThanOrEqualTo(DateTime.UtcNow.AddDays(1)));
        this._backgroundJobClient.Received(1)
            .Create(Arg.Is<Job>(j => j.Method.Name == nameof(IFriendService.GetFriends)), Arg.Any<EnqueuedState>());
    }

    [Test]
    public async Task SaveTwitchLogin_WhenLinkExists_UpdatesTokensAndUsernameAndEnablesSettings()
    {
        await using var db = CreateDatabase();
        AddUser(db, 1, "alice", new SettingsModel { EnableTwitch = false });
        db.Twitch.Add(new TwitchModel
        {
            UserId       = 1,
            TwitchId     = 55,
            Username     = "old_tv",
            AccessToken  = Protector.Protect("old-access"),
            RefreshToken = Protector.Protect("old-refresh")
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.SaveTwitchLogin(1, 99, "new_tv", "new-access", "new-refresh");

        var twitch   = await db.Twitch.SingleAsync();
        var settings = await db.Settings.SingleAsync();
        Assert.That(twitch.Username,                          Is.EqualTo("new_tv"));
        Assert.That(twitch.TwitchId,                          Is.EqualTo(99));
        Assert.That(Protector.Unprotect(twitch.AccessToken),  Is.EqualTo("new-access"));
        Assert.That(Protector.Unprotect(twitch.RefreshToken), Is.EqualTo("new-refresh"));
        Assert.That(settings.EnableTwitch,                    Is.True);
    }

    [Test]
    public async Task SaveTwitchLogin_WhenLinkIsNew_CreatesLinkAndEnablesSettings()
    {
        await using var db = CreateDatabase();
        AddUser(db, 1, "alice", new SettingsModel { EnableTwitch = false });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.SaveTwitchLogin(1, 55, "alice_tv", "access", "refresh");

        var twitch   = await db.Twitch.SingleAsync();
        var settings = await db.Settings.SingleAsync();
        Assert.That(twitch.UserId,                            Is.EqualTo(1));
        Assert.That(twitch.TwitchId,                          Is.EqualTo(55));
        Assert.That(twitch.Username,                          Is.EqualTo("alice_tv"));
        Assert.That(Protector.Unprotect(twitch.AccessToken),  Is.EqualTo("access"));
        Assert.That(Protector.Unprotect(twitch.RefreshToken), Is.EqualTo("refresh"));
        Assert.That(settings.EnableTwitch,                    Is.True);
    }
}
