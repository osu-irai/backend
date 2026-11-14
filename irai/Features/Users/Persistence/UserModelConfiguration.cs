using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace osuRequestor.Features.Users.Persistence;

public class UserModelConfiguration : IEntityTypeConfiguration<UserModel>
{
    #region IEntityTypeConfiguration<UserModel> Members

    public void Configure(EntityTypeBuilder<UserModel> builder)
    {
        builder.Property(um => um.Id)
               .IsRequired();
        builder.Property(um => um.CountryCode)
               .IsRequired(false);
        builder.HasOne(e => e.Settings)
               .WithOne(e => e.User);
        builder.HasOne(e => e.TwitchSettings)
               .WithOne(e => e.User);
    }

    #endregion
}
