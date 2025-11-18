using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using osuRequestor.Data.Configuration;
using osuRequestor.Models;

namespace osuRequestor.Data;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<RequestModel> Requests { get; set; } = null!;
    public DbSet<BeatmapModel> Beatmaps { get; set; } = null!;
    public DbSet<BeatmapSetModel> BeatmapSets { get; set; } = null!;
    public DbSet<UserModel> Users { get; set; } = null!;

    public DbSet<SettingsModel> Settings { get; set; } = null;

    public DbSet<TokenModel> Tokens { get; set; } = null!;

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RequestModelConfiguration());
        modelBuilder.ApplyConfiguration(new UserModelConfiguration());
        modelBuilder.ApplyConfiguration(new BeatmapModelConfiguration());
        modelBuilder.ApplyConfiguration(new SettingsModelConfiguration());

        modelBuilder.Entity<UserModel>().HasOne(e => e.Settings).WithOne(e => e.User)
            .HasForeignKey<SettingsModel>(e => e.UserId).IsRequired();
    }

}
