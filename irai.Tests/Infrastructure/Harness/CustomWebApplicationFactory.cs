using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using NSubstitute;
using osuRequestor.Infrastructure.ExternalServices.OsuApi.Interfaces;
using osuRequestor.Infrastructure.Persistence;
using osuRequestor.Tests.Infrastructure.Fixtures;

namespace osuRequestor.Tests.Infrastructure.Harness;

// WebApplicationFactory only needs a type from the application assembly; Program is static.
public sealed class CustomWebApplicationFactory : WebApplicationFactory<DatabaseContext>
{
    private readonly TestDatabaseFixture _database;

    public CustomWebApplicationFactory(TestDatabaseFixture database)
    {
        this._database = database;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var database = new NpgsqlConnectionStringBuilder(this._database.ConnectionString);

            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Host"]                 = database.Host,
                ["Database:Port"]                 = database.Port.ToString(),
                ["Database:Database"]             = database.Database,
                ["Database:Username"]             = database.Username,
                ["Database:Password"]             = database.Password,
                ["osuApi:ClientId"]               = "1",
                ["osuApi:ClientSecret"]           = "test-osu-secret",
                ["osuApi:CallbackUrl"]            = "/api/oauth/callback",
                ["Twitch:ClientId"]               = "test-twitch-client",
                ["Twitch:ClientSecret"]           = "test-twitch-secret",
                ["Twitch:RedirectUrl"]            = "/api/twitch/auth/callback",
                ["JwtSettings:SecretKey"]         = "test-signing-key-with-at-least-thirty-two-bytes",
                ["JwtSettings:Issuer"]            = "https://tests.local/",
                // Must match the hard-coded audience in the Irc authorization policy.
                ["JwtSettings:Audience"]          = "http://localhost:5076/api/bot",
                ["JwtSettings:ExpirationMinutes"] = "100000",
                ["ServerConfig:HomePage"]         = "https://tests.local/",
                ["Clients:Irc:Name"]              = "Irc",
                ["Clients:Irc:ClientSecret"]      = "irc-test-secret",
                ["Clients:Twitch:Name"]           = "Twitch",
                ["Clients:Twitch:ClientSecret"]   = "twitch-test-secret"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // The HTTP test host does not need RabbitMQ. Keep message publishing
            // resolvable, but replace the broker-backed implementations.
            services.RemoveAll<IPublishEndpoint>();
            services.RemoveAll<ISendEndpointProvider>();
            services.AddSingleton(Substitute.For<IPublishEndpoint>());

            // MessageBusService resolves a send endpoint before sending; give the
            // substitute a non-null endpoint so settings changes can complete.
            var sendEndpoint         = Substitute.For<ISendEndpoint>();
            var sendEndpointProvider = Substitute.For<ISendEndpointProvider>();
            sendEndpointProvider.GetSendEndpoint(Arg.Any<Uri>())
                                .Returns(Task.FromResult(sendEndpoint));
            services.AddSingleton(sendEndpointProvider);

            // Avoid real osu! network calls from OsuApiProvider's constructor.
            services.RemoveAll<IOsuApiProvider>();
            services.AddSingleton(Substitute.For<IOsuApiProvider>());

            var massTransitHostedServices = services
                                            .Where(descriptor =>
                                                       descriptor.ServiceType == typeof(IHostedService) &&
                                                       descriptor.ImplementationType?.FullName?.Contains(
                                                           "MassTransit", StringComparison.Ordinal) == true)
                                            .ToList();

            foreach (var descriptor in massTransitHostedServices)
            {
                services.Remove(descriptor);
            }
        });
    }
}
