using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace osuRequestor.Data;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<Models.RequestModel> Requests { get; set; } = null!;
    public DbSet<Models.BeatmapModel> Beatmaps { get; set; } = null!;
    public DbSet<Models.BeatmapSetModel> BeatmapSets { get; set; } = null!;
    public DbSet<Models.UserModel> Users { get; set; } = null!;

    public DbSet<Models.TokenModel> Tokens { get; set; } = null!;

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Models.RequestModel>()
            .Property(request => request.Date);
        
        modelBuilder.Entity<Models.BeatmapModel>()
            .Property(item => item.Id)
            .IsRequired();

        modelBuilder.Entity<Models.UserModel>()
            .Property(item => item.CountryCode)
            .IsRequired(false);

    }

}