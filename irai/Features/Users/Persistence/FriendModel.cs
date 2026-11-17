using Microsoft.EntityFrameworkCore;

namespace osuRequestor.Features.Users.Persistence;

[PrimaryKey(nameof(UserId), nameof(FriendId))]
public class FriendModel
{
    public required int UserId { get; set; }

    /// <summary>
    /// User's model
    /// <remarks>Semantically guaranteed to exist</remarks>
    /// </summary>
    public UserModel User { get; set; }

    /// <summary>
    /// Friend user's ID
    /// <remarks>Always present even if the user is not registered</remarks>
    /// </summary>
    public int? FriendId { get; set; }
}
