using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using osuRequestor.Infrastructure.Persistence.Models;

namespace osuRequestor.Infrastructure.Persistence.Configurations;

public class UserModelConfiguration : IEntityTypeConfiguration<UserModel>
{
    #region IEntityTypeConfiguration<UserModel> Members

    public void Configure(EntityTypeBuilder<UserModel> builder)
    {
        builder.Property(um => um.Id)
               .IsRequired();
        builder.Property(um => um.CountryCode)
               .IsRequired(false);
    }

    #endregion
}
