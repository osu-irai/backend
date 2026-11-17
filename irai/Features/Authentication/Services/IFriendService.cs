namespace osuRequestor.Features.Authentication.Services;

public interface IFriendService
{
    public Task GetFriends(int userId, string token);
}
