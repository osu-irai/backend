using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using osu.Game.Extensions;
using osuRequestor.Infrastructure.Configuration;

namespace osuRequestor.Infrastructure.DependencyInjection;

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
        services.AddAuthorization(options =>
        {
            options.AddPolicy("Twitch",
                              policy =>
                              {
                                  policy.RequireClaim("aud", "http://localhost:5076/api/bot/twitch/",
                                                      "http://localhost:5076/api/bot/twitch");
                              });
            options.AddPolicy(
                "Irc",
                policy =>
                {
                    policy.RequireClaim("aud", "http://localhost:5076/api/bot/irc/",
                                        "http://localhost:5076/api/bot/irc");
                });
        });
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(_ => { });
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<AuthConfig>, IOptions<AuthClientsConfig>>((jwt, authConfig, clients) =>
                {
                    var cfg        = authConfig.Value;
                    var clientDict = clients.Value;
                    var audiences = clientDict.Values.Select(v => $"{cfg.Audience}/{v.Name.ToLower()}")
                                              .ToList();
                    var securityKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(cfg.SecretKey ?? throw new NotImplementedException()));
                    jwt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer           = true,
                        ValidateAudience         = false,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer              = cfg.Issuer,
                        // ValidAudiences = audiences,
                        IssuerSigningKey         = securityKey
                    };
                })
                .ValidateOnStart();
    }

    private static void AddOsuAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication("InternalCookies")
                .AddCookie("InternalCookies", options =>
                {
                    // set some paths to empty to make auth not redirect API calls
                    options.LoginPath           = "";
                    options.AccessDeniedPath    = "";
                    options.LogoutPath          = "";
                    options.Cookie.Path         = "/";
                    options.Cookie.Name         = "iraiLogin";
                    options.Cookie.HttpOnly     = false;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.SlidingExpiration   = true;
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
                .AddCookie("Cookies", options => { options.LoginPath = "/api/twitch/auth"; })
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
                    options.Scope.AddRange(["user:read:chat", "user:write:chat", "user:bot", "channel:bot"]);

                    options.SaveTokens = true;

                    options.Validate();
                })
                .ValidateOnStart();
    }
}
