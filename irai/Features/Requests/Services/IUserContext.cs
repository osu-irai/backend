using irai.Features.Requests.Models;

namespace irai.Features.Requests.Services;

public interface IUserContext
{
    public int? GetCurrentUserId(PostRequestRequest body);
}
