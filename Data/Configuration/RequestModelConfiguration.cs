using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using osuRequestor.Models;

namespace osuRequestor.Data.Configuration;

public class RequestModelConfiguration : IEntityTypeConfiguration<RequestModel>
{
    public void Configure(EntityTypeBuilder<RequestModel> builder)
    {
        builder.Property(rm => rm.Date).IsRequired();
    }
}