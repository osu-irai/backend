using System.Security.Claims;
using irai.Common.Constants;
using irai.Common.Exceptions;
using irai.Features.Authentication.Persistence;
using irai.Infrastructure.ExternalServices.OsuApi;
using irai.Infrastructure.ExternalServices.OsuApi.Interfaces;
using irai.Infrastructure.ExternalServices.OsuApi.Models;
using irai.Infrastructure.Persistence;
using irai.Tests.Infrastructure.Fixtures;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace irai.Tests.Infrastructure.ExternalServices;

[TestFixture]
public class OsuDatabaseAccessTokenProviderTests
{
    private static readonly IDataProtectionProvider Protection = DataProtectionProvider.Create("irai-tests");
    private static readonly IDataProtector Protector = Protection.CreateProtector(TokenProtection.PURPOSE);

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
            Protection,
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
            AccessToken  = Protector.Protect(accessToken),
            RefreshToken = Protector.Protect("old-refresh"),
            Expires      = expires,
            User         = user
        });
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenCookieAuthenticationFails_ThrowsUnauthorized()
    {
        await using var db = CreateDatabase();
        var provider = CreateProvider(db, Substitute.For<IOsuApiProvider>(), AuthenticateResult.Fail("bad"));

        await Assert.ThrowsAsync<UnauthorizedException>(() => provider.GetAccessTokenAsync(CancellationToken.None));
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenHttpContextIsMissingAndUserlessUnavailable_ThrowsUnauthorized()
    {
        await using var db = CreateDatabase();
        await db.SaveChangesAsync();
        var api = Substitute.For<IOsuApiProvider>();
        api.GetUserlessTokenAsync()
           .Returns((string?)null);
        var provider = new OsuDatabaseAccessTokenProvider(
            db,
            new HttpContextAccessor(),
            api,
            Protection,
            NullLogger<OsuDatabaseAccessTokenProvider>.Instance);

        await Assert.ThrowsAsync<UnauthorizedException>(() => provider.GetAccessTokenAsync(CancellationToken.None));
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenHttpContextIsMissing_ReturnsUserlessToken()
    {
        await using var db = CreateDatabase();
        await db.SaveChangesAsync();
        var api = Substitute.For<IOsuApiProvider>();
        api.GetUserlessTokenAsync()
           .Returns("userless-access");
        var provider = new OsuDatabaseAccessTokenProvider(
            db,
            new HttpContextAccessor(),
            api,
            Protection,
            NullLogger<OsuDatabaseAccessTokenProvider>.Instance);

        var token = await provider.GetAccessTokenAsync(CancellationToken.None);

        Assert.That(token, Is.EqualTo("userless-access"));
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenIdentityIsNull_ThrowsArgumentException()
    {
        await using var db = CreateDatabase();
        var result = AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(), "InternalCookies"));
        var provider = CreateProvider(db, Substitute.For<IOsuApiProvider>(), result);

        var exception
            = await Assert.ThrowsAsync<ArgumentException>(() => provider.GetAccessTokenAsync(CancellationToken.None));

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
            = await Assert.ThrowsAsync<ArgumentException>(() => provider.GetAccessTokenAsync(CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("Invalid user identity"));
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenRefreshFails_ThrowsUnauthorized()
    {
        await using var db = CreateDatabase();
        AddUserToken(db, 7, "old-access", DateTime.UtcNow.AddHours(-1));
        await db.SaveChangesAsync();
        var api = Substitute.For<IOsuApiProvider>();
        api.RefreshToken("old-refresh", "old-access")
           .Returns(Task.FromResult<TokenResponse?>(null));
        var provider = CreateProvider(db, api, "7");

        await Assert.ThrowsAsync<UnauthorizedException>(() => provider.GetAccessTokenAsync(CancellationToken.None));
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
        Assert.That(Protector.Unprotect(stored.AccessToken), Is.EqualTo("new-access"));
        Assert.That(stored.Expires,                          Is.GreaterThan(DateTime.UtcNow));
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

        await Assert.ThrowsAsync<NullReferenceException>(() => provider.GetAccessTokenAsync(CancellationToken.None));
    }
}
