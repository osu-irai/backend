using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace osuRequestor.Features.Requests.Persistence;

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
