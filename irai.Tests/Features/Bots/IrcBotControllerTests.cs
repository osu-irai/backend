using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using osuRequestor.Features.Bots.Controllers;
using osuRequestor.Features.Bots.Services;
using osuRequestor.Infrastructure.Persistence;
using osuRequestor.Infrastructure.Persistence.Models;

namespace osuRequestor.Tests.Features.Bots;

[TestFixture]
public class IrcBotControllerTests
{
    private static void AddUser(DatabaseContext db, int id, string username, bool enableIrc)
    {
        db.Users.Add(new UserModel
        {
            Id          = id,
            Username    = username,
            AvatarUrl   = $"https://a.ppy.sh/{id}",
            CountryCode = "US",
            Settings    = new SettingsModel { EnableIrc = enableIrc }
        });
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
    /// No users with enabled IRC integration
    /// </summary>
    [Test]
    public async Task GetNames_WhenAllUsersDisabled_ReturnsEmpty()
    {
        await using var db = CreateDatabase();
        AddUser(db, 1, "alice", false);
        AddUser(db, 2, "bob",   false);
        await db.SaveChangesAsync();
        var service    = new BotService(db);
        var controller = new IrcBotController(service);

        var names = await controller.GetNames();

        Assert.That(names, Is.Empty);
    }

    /// <summary>
    /// Empty database path
    /// </summary>
    [Test]
    public async Task GetNames_WhenNoUsersExist_ReturnsEmpty()
    {
        await using var db         = CreateDatabase();
        var             service    = new BotService(db);
        var             controller = new IrcBotController(service);

        var names = await controller.GetNames();

        Assert.That(names, Is.Empty);
    }

    /// <summary>
    /// Drop users without enabled IRC
    /// </summary>
    [Test]
    public async Task GetNames_WhenUserHasNoSettings_DoesNotIncludeThem()
    {
        await using var db = CreateDatabase();
        db.Users.Add(new UserModel
        {
            Id          = 1,
            Username    = "alice",
            AvatarUrl   = "https://a.ppy.sh/1",
            CountryCode = "US",
            Settings    = null
        });
        await db.SaveChangesAsync();
        var service    = new BotService(db);
        var controller = new IrcBotController(service);

        var names = await controller.GetNames();

        Assert.That(names, Does.Not.Contain("alice"));
    }

    /// <summary>
    /// Some users with enabled IRC integration
    /// </summary>
    [Test]
    public async Task GetNames_WithMixedSettings_ReturnsOnlyEnabledUsers()
    {
        await using var db = CreateDatabase();
        AddUser(db, 1, "alice", true);
        AddUser(db, 2, "bob",   false);
        AddUser(db, 3, "carol", true);
        await db.SaveChangesAsync();
        var service    = new BotService(db);
        var controller = new IrcBotController(service);

        var names = await controller.GetNames();

        Assert.That(names, Is.EquivalentTo(new[] { "alice", "carol" }));
    }
}
