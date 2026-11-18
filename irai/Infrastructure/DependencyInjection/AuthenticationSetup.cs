using System.Text;
using irai.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace irai.Infrastructure.DependencyInjection;

public static class AuthenticationSetup
{
    public static void AddAuthenticationSetup(this IServiceCollection services)
    {
        services.AddJwtAuthentication();
        services.AddOsuAuthentication();
        services.AddTwitchAuthentication();
    }

    private static void AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthorization();
        services.AddOptions<AuthorizationOptions>()
                .Configure<IOptions<AuthConfig>, IOptions<AuthClientsConfig>>((options, authConfig, clients) =>
                {
                    var cfg = authConfig.Value;
                    foreach (var (policyName, client) in clients.Value)
                    {
                        var audience = GetAudience(cfg, client);
                        options.AddPolicy(policyName,
                                          policy => policy.RequireClaim("aud", audience, audience + "/"));
                    }
                });
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(_ => { });
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<AuthConfig>, IOptions<AuthClientsConfig>>((jwt, authConfig, clients) =>
                {
                    var cfg        = authConfig.Value;
                    var clientDict = clients.Value;
                    var audiences = clientDict.Values.Select(v => GetAudience(cfg, v))
                                              .ToList();
                    var securityKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(cfg.SecretKey ?? throw new NotImplementedException()));
                    jwt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer           = true,
                        ValidateAudience         = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer              = cfg.Issuer,
                        ValidAudiences           = audiences,
                        IssuerSigningKey         = securityKey
                    };
                })
                .ValidateOnStart();
    }

    private static string GetAudience(AuthConfig cfg, AuthClient client)
    {
        return $"{cfg.Audience.TrimEnd('/')}/{client.Name.ToLowerInvariant()}";
    }

    private static void AddOsuAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication("InternalCookies")
                .AddCookie("InternalCookies", options =>
                {
                    // set some paths to empty to make auth not redirect API calls
                    options.LoginPath = "";
                    options.AccessDeniedPath = "";
                    options.LogoutPath = "";
                    options.Cookie.Path = "/";
                    options.Cookie.Name = "iraiLogin";
                    options.Cookie.HttpOnly = false;
                    options.Cookie.SameSite = SameSiteMode.Lax; // OAuth callback is cross-site, strict doesn't work 
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.SlidingExpiration = true;
                    options.Events.OnValidatePrincipal = context =>
                    {
                        var name = context.Principal?.Identity?.Name;
                        if (string.IsNullOrEmpty(name) || !long.TryParse(name, out var _))
                        {
                            context.RejectPrincipal();
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        }

                        return Task.CompletedTask;
                    };

                    static Task UnauthorizedRedirect(RedirectContext<CookieAuthenticationOptions> context)
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                        return Task.CompletedTask;
                    }

                    options.Events.OnRedirectToLogin        = UnauthorizedRedirect;
                    options.Events.OnRedirectToAccessDenied = UnauthorizedRedirect;
                })
                .AddCookie("ExternalCookies")
                .AddOAuth("osu", _ => { });

        services.AddOptions<OAuthOptions>("osu")
                .Configure<IOptions<OsuApiConfig>>((options, config) =>
                {
                    var cfg = config.Value;
                    options.SignInScheme = "ExternalCookies";

                    options.TokenEndpoint         = "https://osu.ppy.sh/oauth/token";
                    options.AuthorizationEndpoint = "https://osu.ppy.sh/oauth/authorize";
                    options.ClientId              = cfg.ClientId.ToString();
                    options.ClientSecret          = cfg.ClientSecret;
                    options.CallbackPath          = cfg.CallbackUrl;
                    options.Scope.Add("public");
                    options.Scope.Add("friends.read");
                    options.Scope.Add("identify");

                    options.CorrelationCookie.SameSite = SameSiteMode.Lax;

                    options.SaveTokens = true;

                    options.Validate();
                })
                .ValidateOnStart();
    }

    private static void AddTwitchAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(options =>
                {
                    options.DefaultScheme          = "Cookies";
                    options.DefaultChallengeScheme = "Twitch";
                })
                .AddCookie("Cookies", options => { options.LoginPath = "/twitch/auth"; })
                .AddOAuth("Twitch", _ => { });

        services.AddOptions<OAuthOptions>("Twitch")
                .Configure<IOptions<TwitchConfig>>((options, config) =>
                {
                    var cfg = config.Value;
                    options.TokenEndpoint         = "https://id.twitch.tv/oauth2/token";
                    options.AuthorizationEndpoint = "https://id.twitch.tv/oauth2/authorize";
                    options.ClientId              = cfg.ClientId;
                    options.ClientSecret          = cfg.ClientSecret;
                    options.CallbackPath          = cfg.RedirectUrl;
                    options.Scope.Add("user:read:chat");
                    options.Scope.Add("user:write:chat");
                    options.Scope.Add("user:bot");
                    options.Scope.Add("channel:bot");

                    options.SaveTokens = true;

                    options.Validate();
                })
                .ValidateOnStart();
    }
}
