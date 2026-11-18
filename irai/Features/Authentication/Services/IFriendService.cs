namespace irai.Features.Authentication.Services;

public interface IFriendService
{
    public Task GetFriends(int userId);
}
