using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using osu.NET;
using osu.NET.Authorization;
using osuRequestor.Features.Requests.Services;
using osuRequestor.Infrastructure.Persistence;
using osuRequestor.Infrastructure.Persistence.Models;

namespace osuRequestor.Tests.Features.Requests;

[TestFixture]
public class SearchServiceTests
{
    private static SearchService CreateService(DatabaseContext db)
    {
        var osuClient = new OsuApiClient(
            Substitute.For<IOsuAccessTokenProvider>(),
            NullLogger<OsuApiClient>.Instance);

        return new SearchService(db, osuClient, NullLogger<SearchService>.Instance);
    }

    private static DatabaseContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
                      .UseInMemoryDatabase(Guid.NewGuid().ToString())
                      .Options;

        return new DatabaseContext(options);
    }

    private static void AddUser(DatabaseContext db, int id, string username)
    {
        db.Users.Add(new UserModel
        {
            Id          = id,
            Username    = username,
            AvatarUrl   = $"https://a.ppy.sh/{id}",
            CountryCode = "US"
        });
    }

    [Test]
    public async Task SearchPlayers_WithNullQuery_ReturnsAllUsersOrderedByName()
    {
        await using var db = CreateDatabase();
        AddUser(db, 1, "carol");
        AddUser(db, 2, "alice");
        AddUser(db, 3, "bob");
        await db.SaveChangesAsync();

        var result = await CreateService(db).SearchPlayers(null);

        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result.Players.Select(p => p.Username), Is.EqualTo(new[] { "alice", "bob", "carol" }));
    }

    [Test]
    public async Task SearchPlayers_WithPrefix_IsCaseInsensitive()
    {
        await using var db = CreateDatabase();
        AddUser(db, 1, "Alice");
        AddUser(db, 2, "bob");
        await db.SaveChangesAsync();

        var result = await CreateService(db).SearchPlayers("al");

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.Players.Single().Username, Is.EqualTo("Alice"));
    }

    [Test]
    public async Task SearchPlayers_WithNoMatch_ReturnsEmpty()
    {
        await using var db = CreateDatabase();
        AddUser(db, 1, "alice");
        await db.SaveChangesAsync();

        var result = await CreateService(db).SearchPlayers("zzz");

        Assert.That(result.Count, Is.EqualTo(0));
        Assert.That(result.Players, Is.Empty);
    }

    [Test]
    public async Task SearchPlayers_ReturnsAtMostTenMatches()
    {
        await using var db = CreateDatabase();
        for (var i = 0; i < 12; i++)
        {
            AddUser(db, i + 1, $"user{i:00}");
        }

        await db.SaveChangesAsync();

        var result = await CreateService(db).SearchPlayers("user");

        Assert.That(result.Count, Is.EqualTo(10));
        Assert.That(result.Players.Select(p => p.Username), Is.EqualTo(new[]
        {
            "user00", "user01", "user02", "user03", "user04",
            "user05", "user06", "user07", "user08", "user09"
        }));
    }
}
