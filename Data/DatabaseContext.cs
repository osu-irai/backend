using Microsoft.EntityFrameworkCore;

namespace osuRequestor.Data;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public DbSet<Models.RequestModel> Requests { get; set; } = null!;
    
}