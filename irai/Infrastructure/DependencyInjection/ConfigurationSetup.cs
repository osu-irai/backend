using osuRequestor.Infrastructure.Configuration;

namespace osuRequestor.Infrastructure.DependencyInjection;

public static class ConfigurationSetup
{
    public static void AddConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<DatabaseConfig>()
               .Bind(builder.Configuration.GetSection(DatabaseConfig.Position))
               .ValidateDataAnnotations()
               .ValidateOnStart();

        builder.Services.AddOptions<OsuApiConfig>()
               .Bind(builder.Configuration.GetSection(OsuApiConfig.Position))
               .ValidateDataAnnotations()
               .ValidateOnStart();

        builder.Services.AddOptions<ServerConfig>()
               .Bind(builder.Configuration.GetSection(ServerConfig.Position))
               .ValidateDataAnnotations()
               .ValidateOnStart();

        builder.Services.AddOptions<AuthConfig>()
               .Bind(builder.Configuration.GetSection(AuthConfig.Position))
               .ValidateDataAnnotations()
               .ValidateOnStart();

        builder.Services.AddOptions<TwitchConfig>()
               .Bind(builder.Configuration.GetSection(TwitchConfig.Position))
               .ValidateDataAnnotations()
               .ValidateOnStart();

        builder.Services.AddOptions<AuthClientsConfig>()
               .Bind(builder.Configuration.GetSection(AuthClientsConfig.Position))
               .Validate(clients => clients is { Count: > 0 } &&
                                    clients.All(client =>
                                                    !string.IsNullOrWhiteSpace(client.Key)        &&
                                                    !string.IsNullOrWhiteSpace(client.Value.Name) &&
                                                    !string.IsNullOrWhiteSpace(client.Value.ClientSecret)),
                         "Clients must contain at least one entry with a non-empty key, Name, and ClientSecret.")
               .ValidateOnStart();
    }
}
