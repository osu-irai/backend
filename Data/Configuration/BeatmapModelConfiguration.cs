using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using osuRequestor.Models;

namespace osuRequestor.Data.Configuration;

public class BeatmapModelConfiguration : IEntityTypeConfiguration<BeatmapModel>
{
    public void Configure(EntityTypeBuilder<BeatmapModel> builder)
    {
        builder.Property(bm => bm.Id).IsRequired();
    }
}