using System.Net;
using irai.Features.Authentication.Persistence;
using irai.Features.Users.Models;
using irai.Infrastructure.Persistence;
using irai.Tests.Infrastructure.Factories;
using irai.Tests.Infrastructure.Fixtures;
using irai.Tests.Infrastructure.Harness;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace irai.Tests.Features.Users;

[TestFixture]
[Category("Integration")]
public class UsersIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task GetOwnSettings_WithInternalCookie_ReturnsStoredSettings()
    {
        await SeedAsync(db =>
        {
            db.Users.Add(TestDataBuilder.User(1, "alice", true));
            return Task.CompletedTask;
        });

        using var client = AuthenticatedTestClient.CreateInternalCookiesClient(this.Factory, 1);

        var settings = await client.GetFromJsonAsync<SettingsDTO>("/users/own/settings");

        Assert.That(settings,              Is.Not.Null);
        Assert.That(settings!.EnableIrc,   Is.True);
        Assert.That(settings.EnableTwitch, Is.False);
    }

    [Test]
    public async Task GetOwnTwitch_WithTwitchLink_ReturnsUsername()
    {
        await SeedAsync(db =>
        {
            var user = TestDataBuilder.User(1, "alice", enableTwitch: true);
            db.Users.Add(user);
            db.Twitch.Add(new TwitchModel
            {
                User         = user,
                UserId       = user.Id,
                TwitchId     = 1234,
                Username     = "alice_tv",
                AccessToken  = "access",
                RefreshToken = "refresh"
            });

            return Task.CompletedTask;
        });

        using var client = AuthenticatedTestClient.CreateInternalCookiesClient(this.Factory, 1);

        var response = await client.GetAsync("/users/own/twitch");
        response.EnsureSuccessStatusCode();

        // Controller returns Ok(string); ASP.NET's string formatter writes it as plain text.
        var username = await response.Content.ReadAsStringAsync();
        Assert.That(username, Is.EqualTo("alice_tv"));
    }

    [Test]
    public async Task GetOwnTwitch_WithoutTwitchLink_ReturnsUnauthorized()
    {
        await SeedAsync(db =>
        {
            db.Users.Add(TestDataBuilder.User(1, "alice"));
            return Task.CompletedTask;
        });

        using var client = AuthenticatedTestClient.CreateInternalCookiesClient(this.Factory, 1);

        var response = await client.GetAsync("/users/own/twitch");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GetOwnUser_WithInternalCookie_ReturnsUserAndRequestCount()
    {
        await SeedAsync(db =>
        {
            var user    = TestDataBuilder.User(1, "alice");
            var set     = TestDataBuilder.BeatmapSet(10);
            var beatmap = TestDataBuilder.Beatmap(100, set);

            db.Users.Add(user);
            db.BeatmapSets.Add(set);
            db.Beatmaps.Add(beatmap);
            db.Requests.Add(TestDataBuilder.Request(user, null, beatmap));

            return Task.CompletedTask;
        });

        using var client = AuthenticatedTestClient.CreateInternalCookiesClient(this.Factory, 1);

        var response = await client.GetAsync("/users/own");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SelfUserResponse>();

        Assert.That(result!.User.Id,      Is.EqualTo(1));
        Assert.That(result.User.Username, Is.EqualTo("alice"));
        Assert.That(result.RequestCount,  Is.EqualTo(1));
    }

    [Test]
    public async Task OwnSettings_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await this.Client.GetAsync("/users/own/settings");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task OwnTwitch_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await this.Client.GetAsync("/users/own/twitch");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task OwnUser_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await this.Client.GetAsync("/users/own");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task PostOwnSettings_WithInternalCookie_PersistsChanges()
    {
        await SeedAsync(db =>
        {
            db.Users.Add(TestDataBuilder.User(1, "alice"));
            return Task.CompletedTask;
        });

        using var client = AuthenticatedTestClient.CreateInternalCookiesClient(this.Factory, 1);

        var response = await client.PostAsJsonAsync("/users/own/settings", new
        {
            enableIrc    = true,
            enableTwitch = true
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var scope  = this.Factory.Services.CreateScope();
        var       db     = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var       stored = await db.Settings.SingleAsync(s => s.UserId == 1);

        Assert.That(stored.EnableIrc,    Is.True);
        Assert.That(stored.EnableTwitch, Is.True);
    }
}
