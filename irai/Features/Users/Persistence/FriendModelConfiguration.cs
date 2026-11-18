using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace irai.Features.Users.Persistence;

public class FriendModelConfiguration : IEntityTypeConfiguration<FriendModel>
{
    #region IEntityTypeConfiguration<FriendModel> Members

    public void Configure(EntityTypeBuilder<FriendModel> builder)
    {
        builder.HasOne(f => f.User)
               .WithMany(f => f.Friends)
               .HasForeignKey(f => f.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }

    #endregion
}
