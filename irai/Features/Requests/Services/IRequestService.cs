using irai.Features.Requests.Models;
using irai.Features.Requests.Persistence;
using irai.Features.Users.Persistence;

namespace irai.Features.Requests.Services;

public interface IRequestService
{
    Task<Result<BeatmapModel, RequestServiceError>> GetBeatmap(int beatmapId);
    Task<Result<UserModel, RequestServiceError>> GetUser(int userId);
    Task<Result<UserModel, RequestServiceError>> GetUser(string userName);
    Task<List<ReceivedRequestResponse>> GetRequestsToUser(int id);
    Task<List<SentRequestResponse>> GetRequestsByUser(int id);
    Task DeleteRequest(int id);

    Task<Result<int, RequestServiceError>> CreateRequest(int? sourceId, int destinationId, int beatmapId,
                                                         RequestSource source = RequestSource.Website);

    Task<Result<int, RequestServiceError>> CreateRequest(int? sourceId, string destinationName, int beatmapId,
                                                         RequestSource source = RequestSource.Website);
}
