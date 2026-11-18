using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace irai.Features.Requests.Persistence;

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
