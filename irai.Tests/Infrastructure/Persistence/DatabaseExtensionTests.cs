using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using osuRequestor.Infrastructure.Persistence;
using osuRequestor.Infrastructure.Persistence.Extensions;
using osuRequestor.Infrastructure.Persistence.Models;
using osuRequestor.Tests.Infrastructure.Fixtures;

namespace osuRequestor.Tests.Infrastructure.Persistence;

[TestFixture]
public class DatabaseExtensionTests
{
    private static DatabaseContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
                      .UseInMemoryDatabase(Guid.NewGuid()
                                               .ToString())
                      .Options;

        return new DatabaseContext(options);
    }

    private static UserModel AddUser(
        DatabaseContext db,
        int id,
        string username,
        string? accessToken = null)
    {
        var user = TestDataBuilder.User(id, username);
        db.Users.Add(user);
        if (accessToken is not null)
        {
            db.Tokens.Add(new TokenModel
            {
                UserId       = id,
                AccessToken  = accessToken,
                RefreshToken = "refresh-token",
                Expires      = DateTime.UtcNow.AddDays(1),
                User         = user
            });
        }

        return user;
    }

    private static RequestModel NewRequest(
        BeatmapModel beatmap,
        UserModel target,
        UserModel? sender,
        DateTime date,
        bool deleted = false)
    {
        return new RequestModel
        {
            Beatmap       = beatmap,
            RequestedTo   = target,
            RequestedFrom = sender,
            Source        = RequestSource.Website,
            Date          = date,
            IsDeleted     = deleted
        };
    }

    [Test]
    public async Task AddRequest_WhenBeatmapSetAlreadyExists_ReusesStoredSet()
    {
        await using var db        = CreateDatabase();
        var             target    = AddUser(db, 1, "target");
        var             storedSet = TestDataBuilder.BeatmapSet(100, "Stored Artist", "Stored Title");
        db.BeatmapSets.Add(storedSet);
        await db.SaveChangesAsync();
        var incomingSet = TestDataBuilder.BeatmapSet(100, "Incoming Artist", "Incoming Title");

        await db.AddRequest(NewRequest(
                                TestDataBuilder.Beatmap(10, incomingSet),
                                target,
                                null,
                                DateTime.MinValue));

        var requests = await db.Requests
                               .Include(r => r.Beatmap)
                               .ThenInclude(b => b.BeatmapSet)
                               .ToListAsync();
        Assert.That(await db.BeatmapSets.CountAsync(), Is.EqualTo(1));
        Assert.That(requests.Single()
                            .Beatmap.BeatmapSet, Is.SameAs(storedSet));
        Assert.That(requests.Single()
                            .Beatmap.BeatmapSet.Title, Is.EqualTo("Stored Title"));
    }

    [Test]
    public async Task AddRequest_WhenDuplicate_UpdatesDateInsteadOfAdding()
    {
        await using var db     = CreateDatabase();
        var             target = AddUser(db, 1, "target");
        await db.SaveChangesAsync();
        var beatmap = TestDataBuilder.Beatmap(10, TestDataBuilder.BeatmapSet(100));
        await db.AddRequest(NewRequest(beatmap, target, null, DateTime.MinValue));
        var oldDate  = DateTime.UtcNow.AddDays(-2);
        var existing = await db.Requests.SingleAsync();
        existing.Date = oldDate;
        await db.SaveChangesAsync();

        await db.AddRequest(NewRequest(
                                TestDataBuilder.Beatmap(10, TestDataBuilder.BeatmapSet(100)),
                                target,
                                null,
                                DateTime.MinValue));

        var requests = await db.Requests.ToListAsync();
        Assert.That(requests,         Has.Count.EqualTo(1));
        Assert.That(requests[0].Date, Is.GreaterThan(oldDate));
    }

    [Test]
    public async Task AddRequest_WhenNew_AddsRequest()
    {
        await using var db     = CreateDatabase();
        var             target = AddUser(db, 1, "target");
        await db.SaveChangesAsync();

        await db.AddRequest(NewRequest(
                                TestDataBuilder.Beatmap(10, TestDataBuilder.BeatmapSet(100)),
                                target,
                                null,
                                DateTime.MinValue));

        Assert.That(await db.Requests.CountAsync(),         Is.EqualTo(1));
        Assert.That((await db.Requests.SingleAsync()).Date, Is.Not.EqualTo(DateTime.MinValue));
    }

    [Test]
    public async Task GetBeatmap_IncludesBeatmapSet()
    {
        await using var db  = CreateDatabase();
        var             set = TestDataBuilder.BeatmapSet(10, "Artist", "Title");
        db.Beatmaps.Add(TestDataBuilder.Beatmap(1, set));
        await db.SaveChangesAsync();

        var beatmap = await db.GetBeatmap(1);

        Assert.That(beatmap.IsSome(), Is.True);
        Assert.That(beatmap.Value()
                           .BeatmapSet, Is.Not.Null);
        Assert.That(beatmap.Value()
                           .BeatmapSet.Title, Is.EqualTo("Title"));
    }

    [Test]
    public async Task GetUserByClaim_IncludesToken()
    {
        await using var db = CreateDatabase();
        AddUser(db, 7, "alice", "access-token");
        await db.SaveChangesAsync();

        var user = await db.GetUserByClaim(7);

        Assert.That(user.IsSome(), Is.True);
        Assert.That(user.Value()
                        .Id, Is.EqualTo(7));
        Assert.That(user.Value()
                        .Token, Is.Not.Null);
        Assert.That(user.Value()
                        .Token.AccessToken, Is.EqualTo("access-token"));
    }

    [Test]
    public async Task GetUserByName_MatchesExactlyAndIsCaseSensitive()
    {
        await using var db = CreateDatabase();
        AddUser(db, 7, "Alice");
        await db.SaveChangesAsync();

        var exact     = await db.GetUserByName("Alice");
        var otherCase = await db.GetUserByName("alice");

        Assert.That(exact.IsSome(), Is.True);
        Assert.That(exact.Value()
                         .Id, Is.EqualTo(7));
        Assert.That(otherCase.IsNone(), Is.True);
    }

    [Test]
    public async Task GetUser_WhenIdIsNull_ReturnsNone()
    {
        await using var db = CreateDatabase();
        AddUser(db, 7, "alice");
        await db.SaveChangesAsync();

        var user = await db.GetUser(null);

        Assert.That(user.IsNone(), Is.True);
    }

    [Test]
    public async Task GetUser_WhenUserExists_ReturnsSome()
    {
        await using var db = CreateDatabase();
        AddUser(db, 7, "alice");
        await db.SaveChangesAsync();

        var user = await db.GetUser(7);

        Assert.That(user.IsSome(), Is.True);
        Assert.That(user.Value()
                        .Username, Is.EqualTo("alice"));
    }

    [Test]
    public async Task GetUser_WhenUserIsMissing_ReturnsNone()
    {
        await using var db = CreateDatabase();

        var user = await db.GetUser(404);

        Assert.That(user.IsNone(), Is.True);
    }

    [Test]
    public async Task UpdateToken_UpdatesAccessTokenAndExpiry()
    {
        await using var db = CreateDatabase();
        AddUser(db, 7, "alice", "old-token");
        await db.SaveChangesAsync();
        var expiry = DateTime.UtcNow.AddDays(1);

        await db.UpdateToken(7, new TokenModel
        {
            UserId      = 7,
            AccessToken = "new-token",
            Expires     = expiry
        });

        var token = await db.Tokens.SingleAsync(t => t.UserId == 7);
        Assert.That(token.AccessToken, Is.EqualTo("new-token"));
        Assert.That(token.Expires,     Is.EqualTo(expiry));
    }
}
