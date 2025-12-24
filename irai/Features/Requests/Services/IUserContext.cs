using osuRequestor.Features.Requests.Models;

namespace osuRequestor.Features.Requests.Services;

public interface IUserContext
{
    public int? GetCurrentUserId(PostRequestRequest body);
}
