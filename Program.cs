using Microsoft.EntityFrameworkCore;
using Npgsql;
using osuRequestor.Data;

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
            options.UseNpgsql(connectionString.ConnectionString));

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        builder.Services.AddControllers();
        
        var app = builder.Build();

        app.UseHttpLogging();
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.MapControllers();
        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.Run();
    }
}