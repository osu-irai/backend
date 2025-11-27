using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using osuRequestor.Configuration;
using osuRequestor.Data;
using osuRequestor.Exceptions;
using osuRequestor.Models;
using osuRequestor.Setup;
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

        builder.Services.AddDbContext<DatabaseContext>(options =>
        {
            options.UseNpgsql(databaseConfig.ToConnection().ConnectionString,
                o => o.MapEnum<RequestSource>("Source"));
        });
        builder.Services.AddDataProtection().PersistKeysToDbContext<DatabaseContext>();

        builder.AddConfiguration();
        // TODO: Add rate limiting
        builder.Services.AddServiceSetup();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddControllers();

        builder.Services.AddOpenApi();
        builder.Services.AddSwaggerGen();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHttpClient();
        builder.Services.AddAuthenticationSetup(builder.Configuration);

        var app = builder.Build();


        app.UseCors(options =>
        {
            options.AllowAnyHeader();
            if (app.Environment.IsDevelopment())
                options.WithOrigins("http://localhost:5076", "http://localhost:5077", "http://frontend:5077",
                    "http://irai-dev.comf.ee");
            else
                options.WithOrigins("https://irai.comf.ee");
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