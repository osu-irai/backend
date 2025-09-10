using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using osu.NET;
using osu.NET.Authorization;
using osuRequestor.Apis.OsuApi;
using osuRequestor.Apis.OsuApi.Interfaces;
using osuRequestor.Configuration;
using osuRequestor.Data;
using osuRequestor.Extensions;
using osuRequestor.Persistence;
using osuRequestor.Services;

namespace osuRequestor;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddAuthorization();
        builder.Services.AddHttpLogging(o => { });

        var dbConfig = builder.Configuration.GetSection("Database");
        var osuConfig = builder.Configuration.GetSection("osuApi");

        var connectionString = new NpgsqlConnectionStringBuilder
        {
            Host = dbConfig["Host"],
            Port = int.Parse(dbConfig["Port"]!),
            Database = dbConfig["Database"],
            Username = dbConfig["Username"],
            Password = dbConfig["Password"]
        };
        
        // Add a database
        builder.Services.AddDbContext<DatabaseContext>(options =>
        {
            options.UseNpgsql(connectionString.ConnectionString);
        });
        builder.Services.Configure<OsuApiConfig>(osuConfig);
        // TODO: Add rate limiting
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHttpClient<OsuApiProvider>();
        // builder.Services
            // .AddOsuApiClient(
                // new OsuClientAccessTokenProvider(osuConfig.GetValue<string>("clientId")!,
                    // osuConfig.GetValue<string>("clientSecret")!));
        builder.Services.AddScoped<OsuDatabaseAccessTokenProvider>();
        builder.Services.AddScoped<OsuApiClient>((serviceProvider) =>
        {
            var provider = serviceProvider.GetRequiredService<OsuDatabaseAccessTokenProvider>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            return new OsuApiClient(provider, new(),
                loggerFactory.CreateLogger("UserTokenOsuApiClient") as ILogger<OsuApiClient>);
        });
        builder.Services.AddSingleton<IOsuApiProvider, OsuApiProvider>();
        builder.Services.AddScoped<Repository>();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddControllers();
        
        builder.Services.AddOpenApi();
        builder.Services.AddSwaggerGen();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddAuthentication("InternalCookies")
            .AddCookie("InternalCookies", options =>
            {
                // set some paths to empty to make auth not redirect API calls
                options.LoginPath = string.Empty;
                options.AccessDeniedPath = string.Empty;
                options.LogoutPath = string.Empty;
                options.Cookie.Path = "/";
                options.Cookie.Name = "osuToken";
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
                options.SignInScheme = "ExternalCookies";

                options.TokenEndpoint = "https://osu.ppy.sh/oauth/token";
                options.AuthorizationEndpoint = "https://osu.ppy.sh/oauth/authorize";
                options.ClientId = osuConfig["ClientID"]!;
                options.ClientSecret = osuConfig["ClientSecret"]!;
                options.CallbackPath = osuConfig["CallbackUrl"];
                options.Scope.Add("public");
                options.Scope.Add("friends.read");
                options.Scope.Add("identify");

                options.CorrelationCookie.SameSite = SameSiteMode.Lax;

                options.SaveTokens = true;

                options.Validate();
            });


        var app = builder.Build();


        app.MapControllers();
        app.UseCors(options =>
        {
            options.AllowAnyHeader();
            options.WithOrigins("http://localhost:5077", "http://localhost:5076", "http://127.0.0.1:5077", "http://127.0.0.1:5076");
            options.AllowAnyMethod();
            options.AllowCredentials();
        });

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapSwagger();
            app.UseHttpLogging();
        }

        app.UseAuthentication();
        app.UseAuthorization();
        
        app.Run();
    }
}
