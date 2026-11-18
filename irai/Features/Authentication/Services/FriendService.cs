using irai.Features.Users.Persistence;
using irai.Infrastructure.ExternalServices.OsuApi.Interfaces;
using irai.Infrastructure.Persistence;

namespace irai.Features.Authentication.Services;

public class FriendService(
    DatabaseContext context,
    IOsuApiProvider osuApiProvider) : IFriendService
{
    #region IFriendService Members

    public async Task GetFriends(int userId, string token)
    {
        var friends = await osuApiProvider.GetFriends(token);

        var friendIds = friends.Select(f => new FriendModel
        {
            UserId   = userId,
            FriendId = f.Id
        });

        await context.Friends.AddRangeAsync(friendIds);
        await context.SaveChangesAsync();
    }

    #endregion
}
