using Hangfire;
using irai.Features.Requests.Persistence;
using irai.Infrastructure.Configuration;
using irai.Infrastructure.DependencyInjection;
using irai.Infrastructure.ExceptionHandling;
using irai.Infrastructure.Hangfire;
using irai.Infrastructure.Notifications;
using irai.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

namespace irai;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddExceptionHandler<ApiExceptionHandler>();
        builder.Services.AddAuthorization();
        builder.Services.AddHttpLogging(o =>
        {
            if (!builder.Environment.IsDevelopment())
            {
                return;
            }

            o.LoggingFields = HttpLoggingFields.RequestHeaders | HttpLoggingFields.ResponseHeaders;
            o.RequestHeaders.Add("Origin");
            o.ResponseHeaders.Add("Origin");
            o.ResponseHeaders.Add("Access-Control-Allow-Origin");
        });

        builder.AddConfiguration();
        builder.Services.AddDbContext<DatabaseContext>((serviceProvider, options) =>
        {
            var databaseConfig = serviceProvider.GetRequiredService<IOptions<DatabaseConfig>>()
                                                .Value;
            options.UseNpgsql(databaseConfig.ToConnection()
                                            .ConnectionString,
                              o => o.MapEnum<RequestSource>("Source"));
        });
        builder.Services.AddDataProtection()
               .PersistKeysToDbContext<DatabaseContext>();

        // TODO: Add rate limiting
        builder.Services.AddServiceSetup();

        // Background job processing
        builder.Services.AddHangfire(conf => conf
                                         .UseInMemoryStorage());
        builder.Services.AddHangfireServer();

        builder.Services.AddControllers();

        builder.Services.AddOpenApi();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHttpClient();
        builder.Services.AddAuthenticationSetup();

        var app = builder.Build();

        app.UseCors(options =>
        {
            options.AllowAnyHeader();
            if (app.Environment.IsDevelopment())
            {
                options.WithOrigins("http://localhost:5076", "http://localhost:5174",
                                    "http://irai-dev.comf.ee");
            }
            else
            {
                options.WithOrigins("https://irai.comf.ee");
            }

            options.AllowCredentials();
            options.AllowAnyMethod();
        });
        app.UseExceptionHandler(opt => { });

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            await db.Database.MigrateAsync();
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
            app.UseHttpLogging();
        }

        app.MapHub<NotificationHub>("ws/notifications")
           .RequireAuthorization();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new HangfireDashboardAuthorizationFilter()]
        });
        app.MapControllers();

        app.Run();
    }
}
