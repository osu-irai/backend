using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using osuRequestor.Infrastructure.Persistence.Models;

namespace osuRequestor.Infrastructure.Persistence.Configurations;

public class SettingsModelConfiguration : IEntityTypeConfiguration<SettingsModel>
{
    #region IEntityTypeConfiguration<SettingsModel> Members

    public void Configure(EntityTypeBuilder<SettingsModel> builder)
    {
        builder.Property(o => o.EnableIrc)
               .HasDefaultValue(false);
    }

    #endregion
}
