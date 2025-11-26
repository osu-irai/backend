using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using osu.Game.Extensions;
using osuRequestor.Configuration;

namespace osuRequestor.Setup;

public static class AuthenticationSetup
{
   public static void AddAuthenticationSetup(this IServiceCollection services, IConfiguration configuration)
   {
       services.AddJwtAuthentication(configuration);
       services.AddOsuAuthentication(configuration);
       services.AddTwitchAuthentication(configuration);
   }

   private static void AddJwtAuthentication(this IServiceCollection service, IConfiguration configuration)
   {
       service.AddAuthorization(options =>
       {
           options.AddPolicy("Twitch", policy =>
           {
               policy.RequireClaim("aud", "http://localhost:5076/api/bot/twitch");
           });
           options.AddPolicy("Irc", policy =>
           {
               policy.RequireClaim("aud", "http://localhost:5076/api/bot/irc");
           });
       });
       service.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(jwt =>
       {
           var cfg = new AuthConfig(); 
           configuration.GetSection(AuthConfig.Position).Bind(cfg);
           var clientDict = new Dictionary<string, AuthClient>();
           configuration.GetSection("Clients").Bind(clientDict);
           var audiences = clientDict.Values.Select(v => $"{cfg.Audience}/{v.Name.ToLower()}");
           var securityKey = new SymmetricSecurityKey(
               Encoding.UTF8.GetBytes(cfg.SecretKey ?? throw new NotImplementedException()));
           jwt.TokenValidationParameters = new TokenValidationParameters
           {
               ValidateIssuer = true,
               ValidateAudience = true,
               ValidateIssuerSigningKey = true,
               ValidIssuer = cfg.Issuer,
               ValidAudiences = audiences,
               IssuerSigningKey = securityKey,
           };
       });
   }

   private static void AddOsuAuthentication(this IServiceCollection service, IConfiguration configuration)
   {
       
        service.AddAuthentication("InternalCookies")
            .AddCookie("InternalCookies", options =>
            {
                // set some paths to empty to make auth not redirect API calls
                options.LoginPath = string.Empty;
                options.AccessDeniedPath = string.Empty;
                options.LogoutPath = string.Empty;
                options.Cookie.Path = "/";
                options.Cookie.Name = "iraiLogin";
                options.Cookie.HttpOnly = false;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.SlidingExpiration = true;
                options.Events.OnValidatePrincipal = context =>
                {
                    var name = context.Principal?.Identity?.Name;
                    if (string.IsNullOrEmpty(name) || !long.TryParse(name, out _))
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

                options.Events.OnRedirectToLogin = UnauthorizedRedirect;
                options.Events.OnRedirectToAccessDenied = UnauthorizedRedirect;
            })
            .AddCookie("ExternalCookies")
            .AddOAuth("osu", options =>
            {
                var cfg = configuration.GetSection(OsuApiConfig.Position);
                options.SignInScheme = "ExternalCookies";

                options.TokenEndpoint = "https://osu.ppy.sh/oauth/token";
                options.AuthorizationEndpoint = "https://osu.ppy.sh/oauth/authorize";
                options.ClientId = cfg["ClientId"];
                options.ClientSecret = cfg["ClientSecret"];
                options.CallbackPath = cfg["CallbackUrl"];
                options.Scope.Add("public");
                options.Scope.Add("friends.read");
                options.Scope.Add("identify");

                options.CorrelationCookie.SameSite = SameSiteMode.Lax;

                options.SaveTokens = true;

                options.Validate();
            });
   }

   private static void AddTwitchAuthentication(this IServiceCollection service, IConfiguration configuration)
   {
       var cfg = new TwitchConfig();
       configuration.GetSection(TwitchConfig.Position).Bind(cfg);
       service.AddAuthentication(options =>
           {
               options.DefaultScheme = "Cookies";
               options.DefaultChallengeScheme = "twitch";
           })
           .AddCookie("Cookies", options =>
           {
               options.LoginPath = "/api/twitch/auth";
           })
           .AddOAuth("Twitch", options =>
           {
               options.TokenEndpoint = "https://id.twitch.tv/oauth2/token";
               options.AuthorizationEndpoint = "https://id.twitch.tv/oauth2/authorize";
               options.ClientId = cfg.ClientId;
               options.ClientSecret = cfg.ClientSecret;
               options.CallbackPath = cfg.RedirectUrl;
               options.Scope.AddRange(["user:read:chat", "user:write:chat", "user:bot", "channel:bot"]);

               options.SaveTokens = true;
               
               options.Validate();
           });

   }
}