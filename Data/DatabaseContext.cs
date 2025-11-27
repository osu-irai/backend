using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using osuRequestor.Data.Configuration;
using osuRequestor.Models;

namespace osuRequestor.Data;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options), IDataProtectionKeyContext
{
    /// <summary>
    ///     Table with all beatmap requests
    /// </summary>
    public DbSet<RequestModel> Requests { get; set; } = null!;

    /// <summary>
    ///     Table with all beatmaps stored
    /// </summary>
    public DbSet<BeatmapModel> Beatmaps { get; set; } = null!;

    /// <summary>
    ///     Table with all beatmapsets stored
    /// </summary>
    public DbSet<BeatmapSetModel> BeatmapSets { get; set; } = null!;

    /// <summary>
    ///     Table with osu! users
    /// </summary>
    public DbSet<UserModel> Users { get; set; } = null!;

    /// <summary>
    ///     Table with osu! settings
    /// </summary>
    public DbSet<SettingsModel> Settings { get; set; } = null;

    /// <summary>
    ///     Table with Twitch authentication/integration settings
    /// </summary>
    public DbSet<TwitchModel> Twitch { get; set; }

    /// <summary>
    ///     Table with osu! authentication tokens
    /// </summary>
    public DbSet<TokenModel> Tokens { get; set; } = null!;

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RequestModelConfiguration());
        modelBuilder.ApplyConfiguration(new UserModelConfiguration());
        modelBuilder.ApplyConfiguration(new BeatmapModelConfiguration());
        modelBuilder.ApplyConfiguration(new SettingsModelConfiguration());

        modelBuilder.Entity<UserModel>().HasOne(e => e.Settings).WithOne(e => e.User);
        modelBuilder.Entity<UserModel>().HasOne(e => e.TwitchSettings).WithOne(e => e.User);
    }
}