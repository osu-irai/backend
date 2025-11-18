using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using osuRequestor.Models;

namespace osuRequestor.Data.Configuration;

public class SettingsModelConfiguration : IEntityTypeConfiguration<SettingsModel>
{
    public void Configure(EntityTypeBuilder<SettingsModel> builder)
    {
        builder.Property(o => o.EnableIrc).HasDefaultValue(false);
    }
}