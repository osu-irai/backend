using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using osuRequestor.Models;

namespace osuRequestor.Data.Configuration;

public class UserModelConfiguration : IEntityTypeConfiguration<UserModel>
{
    public void Configure(EntityTypeBuilder<UserModel> builder)
    {
        builder.Property(um => um.Id).IsRequired();
        builder.Property(um => um.CountryCode).IsRequired(false);
    }
}