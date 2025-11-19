using osuRequestor.DTO.Requests;

namespace osuRequestor.Services;

public interface IUserContext
{
    public int? GetCurrentUserId(PostRequestRequest body);
}