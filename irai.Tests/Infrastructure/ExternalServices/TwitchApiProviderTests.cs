using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using osuRequestor.Infrastructure.Configuration;
using osuRequestor.Infrastructure.ExternalServices.TwitchApi;

namespace osuRequestor.Tests.Infrastructure.ExternalServices;

[TestFixture]
public class TwitchApiProviderTests
{
    private static TwitchApiProvider CreateProvider(StubHandler handler)
    {
        return new TwitchApiProvider(
            Options.Create(new TwitchConfig
            {
                ClientId     = "client-id",
                ClientSecret = "client-secret",
                RedirectUrl  = "https://example.com/callback"
            }),
            new HttpClient(handler),
            NullLogger<TwitchApiProvider>.Instance);
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
    public void GetClientCredentials_WhenResponseIsNotSuccessful_ThrowsHttpRequestException()
    {
        var handler  = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));
        var provider = CreateProvider(handler);

        Assert.ThrowsAsync<HttpRequestException>(() => provider.GetClientCredentials());
    }

    [Test]
    public async Task GetClientCredentials_WhenSuccessful_PostsFormFieldsAndReturnsToken()
    {
        var handler = new StubHandler(_ => Json(
                                          """{"access_token":"app-token","expires_in":5000,"token_type":"bearer"}"""));
        var provider = CreateProvider(handler);

        var result = await provider.GetClientCredentials();

        Assert.That(result,              Is.Not.Null);
        Assert.That(result!.AccessToken, Is.EqualTo("app-token"));
        var request = handler.Requests.Single();
        Assert.That(request.Method,          Is.EqualTo(HttpMethod.Post));
        Assert.That(request.RequestUri,      Is.EqualTo(new Uri("https://id.twitch.tv/oauth2/token")));
        Assert.That(handler.Bodies.Single(), Does.Contain("client_id=client-id"));
        Assert.That(handler.Bodies.Single(), Does.Contain("client_secret=client-secret"));
        Assert.That(handler.Bodies.Single(), Does.Contain("grant_type=client_credentials"));
    }

    [Test]
    public void GetUser_WhenResponseIsNotSuccessful_ThrowsHttpRequestException()
    {
        var handler  = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var provider = CreateProvider(handler);

        Assert.ThrowsAsync<HttpRequestException>(() => provider.GetUser("42", "token", "client-id"));
    }

    [Test]
    public async Task GetUser_WhenResponseIsNull_ReturnsEmptyUserList()
    {
        var handler  = new StubHandler(_ => Json("null"));
        var provider = CreateProvider(handler);

        var result = await provider.GetUser("42", "token", "client-id");

        Assert.That(result,      Is.Not.Null);
        Assert.That(result.Data, Is.Empty);
    }

    [Test]
    public async Task GetUser_WhenSuccessful_ReturnsUserAndSendsHeaders()
    {
        var handler = new StubHandler(_ => Json(
                                          """{"data":[{"id":"42","login":"bot","display_name":"Bot","type":"","broadcaster_type":"","description":"","profile_image_url":"","offline_image_url":"","view_count":0}]}"""));
        var provider = CreateProvider(handler);

        var result = await provider.GetUser("42", "token", "client-id");

        Assert.That(result.Data, Has.Count.EqualTo(1));
        Assert.That(result.Data[0].Login, Is.EqualTo("bot"));
        var request = handler.Requests.Single();
        Assert.That(request.Method, Is.EqualTo(HttpMethod.Get));
        Assert.That(request.RequestUri, Is.EqualTo(new Uri("https://api.twitch.tv/helix/users?id=42")));
        Assert.That(request.Headers.Authorization!.Scheme, Is.EqualTo("Bearer"));
        Assert.That(request.Headers.Authorization.Parameter, Is.EqualTo("token"));
        Assert.That(request.Headers.GetValues("Client-Id")
                           .Single(), Is.EqualTo("client-id"));
    }

    [Test]
    public void ValidateUser_WhenResponseIsNotSuccessful_ThrowsHttpRequestException()
    {
        var handler  = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var provider = CreateProvider(handler);

        Assert.ThrowsAsync<HttpRequestException>(() => provider.ValidateUser("token"));
    }

    [Test]
    public async Task ValidateUser_WhenSuccessful_ReturnsValidationAndSendsBearerToken()
    {
        var handler = new StubHandler(_ => Json(
                                          """{"client_id":"client","login":"bot","scopes":["user:read:email"],"user_id":"42","expires_in":3600}"""));
        var provider = CreateProvider(handler);

        var result = await provider.ValidateUser("token");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.UserId, Is.EqualTo("42"));
        var request = handler.Requests.Single();
        Assert.That(request.Method, Is.EqualTo(HttpMethod.Get));
        Assert.That(request.RequestUri, Is.EqualTo(new Uri("https://id.twitch.tv/oauth2/validate")));
        Assert.That(request.Headers.Authorization!.Scheme, Is.EqualTo("Bearer"));
        Assert.That(request.Headers.Authorization.Parameter, Is.EqualTo("token"));
    }
}
