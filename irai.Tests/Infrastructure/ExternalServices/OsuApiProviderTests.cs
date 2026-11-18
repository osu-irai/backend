using System.Net;
using System.Text;
using irai.Common.Extensions;
using irai.Infrastructure.Configuration;
using irai.Infrastructure.ExternalServices.OsuApi;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using osu.NET.Models.Users;

namespace irai.Tests.Infrastructure.ExternalServices;

[TestFixture]
public class OsuApiProviderTests
{
    private static bool HandlerForRefresh(HttpRequestMessage request)
    {
        return request.Content?.ReadAsStringAsync()
                      .GetAwaiter()
                      .GetResult()
                      .Contains("refresh_token") == true;
    }

    private static OsuApiProvider CreateProvider(StubHandler handler)
    {
        return new OsuApiProvider(
            Options.Create(new OsuApiConfig
            {
                ClientId     = 123,
                ClientSecret = "client-secret",
                CallbackUrl  = "https://example.com/callback"
            }),
            new HttpClient(handler),
            NullLogger<OsuApiProvider>.Instance);
    }

    private static HttpResponseMessage Json(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            this.Bodies.Add(request.Content is null
                                ? ""
                                : await request.Content.ReadAsStringAsync(cancellationToken));
            this.Requests.Add(request);
            return responder(request);
        }
    }

    [Test]
    public async Task GetFriends_WhenResponseIsNotSuccessful_ReturnsEmptyArray()
    {
        var handler  = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));
        var provider = CreateProvider(handler);
        var data     = await provider.GetFriends("token");

        Assert.That(data, Is.EqualTo(Array.Empty<User>()));
    }

    [Test]
    public async Task GetFriends_WhenSuccessful_ReturnsUsers()
    {
        var handler = new StubHandler(_ => Json(
                                          """[{"id":7,"username":"alice"},{"id":8,"username":"bob"}]"""));
        var provider = CreateProvider(handler);

        var friends = await provider.GetFriends("token");

        Assert.That(friends, Has.Length.EqualTo(2));
        Assert.That(friends![1].Username, Is.EqualTo("bob"));
        var request = handler.Requests[0];
        Assert.That(request.RequestUri, Is.EqualTo(new Uri("https://osu.ppy.sh/api/v2/friends/")));
        Assert.That(request.Headers.Authorization!.Parameter, Is.EqualTo("token"));
    }

    [Test]
    public async Task GetSelfUser_WhenResponseIsNotSuccessful_ReturnsNone()
    {
        var handler  = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var provider = CreateProvider(handler);
        var data     = await provider.GetSelfUser("token");

        Assert.That(
            data, Is.EqualTo(new Option<irai.Infrastructure.ExternalServices.OsuApi.Models.User>(new None())));
    }

    [Test]
    public async Task GetSelfUser_WhenSuccessful_ReturnsUserAndSendsBearerToken()
    {
        var handler = new StubHandler(_ => Json(
                                              """{"id":7,"username":"alice","country_code":"US","avatar_url":"https://a.ppy.sh/7"}"""));
        var provider = CreateProvider(handler);

        var user = await provider.GetSelfUser("token");

        Assert.That(user.Unwrap(), Is.Not.Null);
        Assert.That(user.Unwrap()
                        .Id, Is.EqualTo(7));
        Assert.That(user.Unwrap()
                        .Username, Is.EqualTo("alice"));
        var request = handler.Requests[0];
        Assert.That(request.Method,                          Is.EqualTo(HttpMethod.Get));
        Assert.That(request.RequestUri,                      Is.EqualTo(new Uri("https://osu.ppy.sh/api/v2/me/")));
        Assert.That(request.Headers.Authorization!.Scheme,   Is.EqualTo("Bearer"));
        Assert.That(request.Headers.Authorization.Parameter, Is.EqualTo("token"));
    }

    [Test]
    public async Task RefreshToken_WhenResponseIsNotSuccessful_ReturnsNull()
    {
        var handler = new StubHandler(request =>
                                          request.Method == HttpMethod.Post && HandlerForRefresh(request)
                                              ? new HttpResponseMessage(HttpStatusCode.BadRequest)
                                              : Json("""{"access_token":"new-access","expires_in":3600}"""));
        var provider = CreateProvider(handler);

        var token = await provider.RefreshToken("refresh-token", "old-access");

        Assert.That(token, Is.Null);
    }

    [Test]
    public async Task GetUserlessTokenAsync_WhenSuccessful_ReturnsTokenAndCachesIt()
    {
        var calls   = 0;
        var handler = new StubHandler(_ =>
        {
            calls++;
            return Json("""{"access_token":"userless","expires_in":3600}""");
        });
        var provider = CreateProvider(handler);

        var first  = await provider.GetUserlessTokenAsync();
        var second = await provider.GetUserlessTokenAsync();

        Assert.That(first,  Is.EqualTo("userless"));
        Assert.That(second, Is.EqualTo("userless"));
        Assert.That(calls,  Is.EqualTo(1));
    }

    [Test]
    public async Task GetUserlessTokenAsync_WhenResponseIsNotSuccessful_ReturnsNull()
    {
        var handler  = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var provider = CreateProvider(handler);

        Assert.That(await provider.GetUserlessTokenAsync(), Is.Null);
    }

    [Test]
    public async Task RefreshToken_WhenSuccessful_PostsRefreshRequestAndReturnsToken()
    {
        var handler  = new StubHandler(_ => Json("""{"access_token":"new-access","expires_in":3600}"""));
        var provider = CreateProvider(handler);

        var token = await provider.RefreshToken("refresh-token", "old-access");

        Assert.That(token,              Is.Not.Null);
        Assert.That(token!.AccessToken, Is.EqualTo("new-access"));
        var request = handler.Requests[0];
        Assert.That(request.Method,     Is.EqualTo(HttpMethod.Post));
        Assert.That(request.RequestUri, Is.EqualTo(new Uri("https://osu.ppy.sh/oauth/token")));
        Assert.That(handler.Bodies[0],  Does.Contain("\"grant_type\":\"refresh_token\""));
        Assert.That(handler.Bodies[0],  Does.Contain("\"refresh_token\":\"refresh-token\""));
        Assert.That(handler.Bodies[0],  Does.Contain("\"access_token\":\"old-access\""));
    }
}
