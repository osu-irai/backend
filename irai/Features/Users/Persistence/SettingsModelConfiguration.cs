using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace irai.Features.Users.Persistence;

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
