using OneOf.Types;
using osuRequestor.Features.Requests.Models;
using osuRequestor.Infrastructure.Persistence.Models;

namespace osuRequestor.Features.Requests.Services;

public interface IRequestService
{
    Task<RequestServiceResult<BeatmapModel>> GetBeatmap(int beatmapId);
    Task<RequestServiceResult<UserModel>> GetUser(int userId);
    Task<RequestServiceResult<UserModel>> GetUser(string userName);
    Task<List<ReceivedRequestResponse>> GetRequestsToUser(int id);
    Task DeleteRequest(int id);

    Task<RequestServiceResult<Ok>> CreateRequest(int? sourceId, int destinationId, int beatmapId,
                                                 RequestSource source = RequestSource.Website);

    Task<RequestServiceResult<Ok>> CreateRequest(int? sourceId, string destinationName, int beatmapId,
                                                 RequestSource source = RequestSource.Website);
}
