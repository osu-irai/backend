using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using osuRequestor.Infrastructure.Persistence.Models;

namespace osuRequestor.Infrastructure.Persistence.Configurations;

public class RequestModelConfiguration : IEntityTypeConfiguration<RequestModel>
{
    #region IEntityTypeConfiguration<RequestModel> Members

    public void Configure(EntityTypeBuilder<RequestModel> builder)
    {
        builder.Property(rm => rm.Date)
               .IsRequired();
    }

    #endregion
}
