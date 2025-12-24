using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using osuRequestor.Common.Exceptions;
using osuRequestor.Infrastructure.ExternalServices.OsuApi;
using osuRequestor.Infrastructure.ExternalServices.OsuApi.Interfaces;
using osuRequestor.Infrastructure.ExternalServices.OsuApi.Models;
using osuRequestor.Infrastructure.Persistence;
using osuRequestor.Infrastructure.Persistence.Models;
using osuRequestor.Tests.Infrastructure.Fixtures;

namespace osuRequestor.Tests.Infrastructure.ExternalServices;

[TestFixture]
public class OsuDatabaseAccessTokenProviderTests
{
    private const int HardCodedUserId = 11482346;

    private static OsuDatabaseAccessTokenProvider CreateProvider(
        DatabaseContext db,
        IOsuApiProvider api,
        string userId)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, userId)],
            "InternalCookies");
        var result = AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), "InternalCookies"));

        return CreateProvider(db, api, result);
    }

    private static OsuDatabaseAccessTokenProvider CreateProvider(
        DatabaseContext db,
        IOsuApiProvider api,
        AuthenticateResult result)
    {
        var authentication = Substitute.For<IAuthenticationService>();
        authentication
            .AuthenticateAsync(Arg.Any<HttpContext>(), Arg.Any<string>())
            .Returns(result);
        var services = new ServiceCollection()
                       .AddSingleton(authentication)
                       .BuildServiceProvider();

        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { RequestServices = services }
        };

        return new OsuDatabaseAccessTokenProvider(
            db,
            accessor,
            api,
            NullLogger<OsuDatabaseAccessTokenProvider>.Instance);
    }

    private static DatabaseContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
                      .UseInMemoryDatabase(Guid.NewGuid()
                                               .ToString())
                      .Options;

        return new DatabaseContext(options);
    }

    private static void AddUserToken(DatabaseContext db, int userId, string accessToken, DateTime expires)
    {
        var user = TestDataBuilder.User(userId, $"user{userId}");
        db.Users.Add(user);
        db.Tokens.Add(new TokenModel
        {
            UserId       = userId,
            AccessToken  = accessToken,
            RefreshToken = "old-refresh",
            Expires      = expires,
            User         = user
        });
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenCookieAuthenticationFails_ThrowsUnauthorized()
    {
        await using var db = CreateDatabase();
        var provider = CreateProvider(db, Substitute.For<IOsuApiProvider>(), AuthenticateResult.Fail("bad"));

        Assert.ThrowsAsync<UnauthorizedException>(() => provider.GetAccessTokenAsync(CancellationToken.None));
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenHttpContextIsMissing_UsesHardCodedUserId()
    {
        await using var db = CreateDatabase();
        AddUserToken(db, HardCodedUserId, "userless-access", DateTime.UtcNow.AddHours(1));
        await db.SaveChangesAsync();
        var api = Substitute.For<IOsuApiProvider>();
        var provider = new OsuDatabaseAccessTokenProvider(
            db,
            new HttpContextAccessor(),
            api,
            NullLogger<OsuDatabaseAccessTokenProvider>.Instance);

        var token = await provider.GetAccessTokenAsync(CancellationToken.None);

        Assert.That(token, Is.EqualTo("userless-access"));
        await api.DidNotReceive()
                 .RefreshToken(Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenIdentityIsNull_ThrowsArgumentException()
    {
        await using var db = CreateDatabase();
        var result = AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(), "InternalCookies"));
        var provider = CreateProvider(db, Substitute.For<IOsuApiProvider>(), result);

        var exception
            = Assert.ThrowsAsync<ArgumentException>(() => provider.GetAccessTokenAsync(CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("Invalid user identity"));
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenNameIsNull_ThrowsArgumentException()
    {
        await using var db = CreateDatabase();
        var result = AuthenticateResult.Success(
            new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity([], "InternalCookies")),
                "InternalCookies"));
        var provider = CreateProvider(db, Substitute.For<IOsuApiProvider>(), result);

        var exception
            = Assert.ThrowsAsync<ArgumentException>(() => provider.GetAccessTokenAsync(CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("Invalid user identity"));
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenTokenIsExpired_RefreshesAndUpdatesDatabase()
    {
        await using var db = CreateDatabase();
        AddUserToken(db, 7, "old-access", DateTime.UtcNow.AddHours(-1));
        await db.SaveChangesAsync();
        var api = Substitute.For<IOsuApiProvider>();
        api.RefreshToken("old-refresh", "old-access")
           .Returns(Task.FromResult<TokenResponse?>(new TokenResponse
           {
               AccessToken = "new-access",
               ExpiresIn   = 3600
           }));
        var provider = CreateProvider(db, api, "7");

        var token = await provider.GetAccessTokenAsync(CancellationToken.None);

        Assert.That(token, Is.EqualTo("new-access"));
        var stored = await db.Tokens.SingleAsync(t => t.UserId == 7);
        Assert.That(stored.AccessToken, Is.EqualTo("new-access"));
        Assert.That(stored.Expires,     Is.GreaterThan(DateTime.UtcNow));
        await api.Received(1)
                 .RefreshToken("old-refresh", "old-access");
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenTokenIsUnexpired_ReturnsStoredToken()
    {
        await using var db = CreateDatabase();
        AddUserToken(db, 7, "stored-access", DateTime.UtcNow.AddHours(1));
        await db.SaveChangesAsync();
        var api      = Substitute.For<IOsuApiProvider>();
        var provider = CreateProvider(db, api, "7");

        var token = await provider.GetAccessTokenAsync(CancellationToken.None);

        Assert.That(token, Is.EqualTo("stored-access"));
        await api.DidNotReceive()
                 .RefreshToken(Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenUserIsMissingFromDatabase_ThrowsNullReferenceFromOption()
    {
        await using var db       = CreateDatabase();
        var             provider = CreateProvider(db, Substitute.For<IOsuApiProvider>(), "999");

        Assert.ThrowsAsync<NullReferenceException>(() => provider.GetAccessTokenAsync(CancellationToken.None));
    }
}
