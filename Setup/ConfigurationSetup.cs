using osuRequestor.Configuration;

namespace osuRequestor.Setup;

public static class ConfigurationSetup
{
    public static void AddConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<OsuApiConfig>(builder.Configuration.GetSection(OsuApiConfig.Position))
            .AddOptionsWithValidateOnStart<OsuApiConfig>().ValidateOnStart();
        builder.Services.Configure<ServerConfig>(builder.Configuration.GetSection(ServerConfig.Position))
            .AddOptionsWithValidateOnStart<ServerConfig>().ValidateOnStart();
        builder.Services.Configure<AuthConfig>(builder.Configuration.GetSection(AuthConfig.Position))
            .AddOptionsWithValidateOnStart<AuthConfig>().ValidateOnStart();
        builder.Services.Configure<TwitchConfig>(builder.Configuration.GetSection(TwitchConfig.Position))
            .AddOptionsWithValidateOnStart<TwitchConfig>().ValidateOnStart();
        var authClients = builder.Configuration.GetSection("Clients");
        builder.Services.Configure<Dictionary<string, AuthClient>>(authClients);
    }
}