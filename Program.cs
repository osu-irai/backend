using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using Npgsql;
using osu.NET;
using osu.NET.Authorization;
using osuRequestor.Apis.OsuApi;
using osuRequestor.Apis.OsuApi.Interfaces;
using osuRequestor.Configuration;
using osuRequestor.Data;
using osuRequestor.Exceptions;
using osuRequestor.Extensions;
using osuRequestor.Models;
using osuRequestor.Persistence;
using osuRequestor.Services;
using osuRequestor.SignalR;

namespace osuRequestor;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddExceptionHandler<ApiExceptionHandler>();
        builder.Services.AddAuthorization();
        builder.Services.AddHttpLogging(o =>
        {
            if (!builder.Environment.IsDevelopment()) return;
            o.LoggingFields = HttpLoggingFields.RequestHeaders | HttpLoggingFields.ResponseHeaders;
            o.RequestHeaders.Add("Origin");
            o.ResponseHeaders.Add("Origin");
            o.ResponseHeaders.Add("Access-Control-Allow-Origin");
        });

        var databaseConfig = new DatabaseConfig();
        builder.Configuration.GetSection(DatabaseConfig.Position).Bind(databaseConfig);
        var authClients = builder.Configuration.GetSection("Clients");
        
        builder.Services.AddDbContext<DatabaseContext>(options =>
        {
            options.UseNpgsql(databaseConfig.ToConnection().ConnectionString, o => o.MapEnum<RequestSource>("Source"));
        });
        builder.Services.AddDataProtection().PersistKeysToDbContext<DatabaseContext>();

        builder.Services.Configure<OsuApiConfig>(builder.Configuration.GetSection(OsuApiConfig.Position));
        builder.Services.Configure<ServerConfig>(builder.Configuration.GetSection(ServerConfig.Position));
        builder.Services.Configure<AuthConfig>(builder.Configuration.GetSection(AuthConfig.Position));
        builder.Services.Configure<Dictionary<string, AuthClient>>(authClients);
        // TODO: Add rate limiting
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHttpClient<OsuApiProvider>();
        builder.Services.AddSignalR();
        builder.Services.AddTransient<RequestService>();
        builder.Services.AddScoped<IRequestNotificationService, RequestNotificationService>();
        builder.Services.AddScoped<IUserContext, HttpUserContext>();
        builder.Services.AddScoped<OsuDatabaseAccessTokenProvider>();
        builder.Services.AddScoped<OsuApiClient>((serviceProvider) =>
        {
            var provider = serviceProvider.GetRequiredService<OsuDatabaseAccessTokenProvider>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            return new OsuApiClient(provider, 
                loggerFactory.CreateLogger("UserTokenOsuApiClient") as ILogger<OsuApiClient>);
        });
        builder.Services.AddSingleton<IOsuApiProvider, OsuApiProvider>();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddControllers();
        
        builder.Services.AddOpenApi();
        builder.Services.AddSwaggerGen();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddAuthorization(options =>
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
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(jwt =>
        {
            var cfg = new AuthConfig(); 
            builder.Configuration.GetSection(AuthConfig.Position).Bind(cfg);
            var clientDict = new Dictionary<string, AuthClient>();
            builder.Configuration.GetSection("Clients").Bind(clientDict);
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
        builder.Services.AddAuthentication("InternalCookies")
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
                var cfg = builder.Configuration.GetSection(OsuApiConfig.Position);
                options.SignInScheme = "ExternalCookies";

                options.TokenEndpoint = "https://osu.ppy.sh/oauth/token";
                options.AuthorizationEndpoint = "https://osu.ppy.sh/oauth/authorize";
                options.ClientId = cfg["ClientId"];
                options.ClientSecret = cfg["ClientSecret"];
                options.CallbackPath = "/api/oauth/callback";
                options.Scope.Add("public");
                options.Scope.Add("friends.read");
                options.Scope.Add("identify");

                options.CorrelationCookie.SameSite = SameSiteMode.Lax;

                options.SaveTokens = true;

                options.Validate();
            });


        var app = builder.Build();


        app.UseCors(options =>
        {
            options.AllowAnyHeader();
            if (app.Environment.IsDevelopment())
            {
                options.WithOrigins("http://localhost:5076", "http://localhost:5077", "http://frontend:5077", "http://irai-dev.comf.ee");
            }
            else
            {
                options.WithOrigins("https://irai.comf.ee");
            }
            options.AllowCredentials();
            options.AllowAnyMethod();
        });
        app.UseExceptionHandler(opt => {});

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            await db.Database.MigrateAsync();
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapSwagger();
            app.UseHttpLogging();
        }
        app.MapHub<NotificationHub>("api/ws/notifications");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        
        app.Run();
    }
}
