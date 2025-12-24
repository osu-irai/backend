using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using osuRequestor.Infrastructure.Persistence.Models;

namespace osuRequestor.Infrastructure.Persistence.Configurations;

public class BeatmapModelConfiguration : IEntityTypeConfiguration<BeatmapModel>
{
    #region IEntityTypeConfiguration<BeatmapModel> Members

    public void Configure(EntityTypeBuilder<BeatmapModel> builder)
    {
        builder.Property(bm => bm.Id)
               .IsRequired();
    }

    #endregion
}
