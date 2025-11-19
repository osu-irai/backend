using System.Diagnostics;
using OneOf;
using OneOf.Types;
using osu.NET;
using osu.NET.Models.Beatmaps;
using osuRequestor.Data;
using osuRequestor.DTO.Responses;
using osuRequestor.Exceptions;
using osuRequestor.Extensions;
using osuRequestor.Models;
using osuRequestor.Persistence;
using osuRequestor.SignalR;

namespace osuRequestor.Services;

public class RequestService
{
    private readonly ILogger<RequestService> _logger;
    private readonly DatabaseContext _dbContext;
    private readonly IRequestNotificationService _notification;
    private readonly OsuApiClient _osuApiClient;

    public RequestService(ILogger<RequestService> logger, DatabaseContext dbContext, OsuApiClient osuApiClient, IRequestNotificationService notification)
    {
        _logger = logger;
        _dbContext = dbContext;
        _notification = notification;
        _osuApiClient = osuApiClient;
    }

    public async Task<RequestServiceResult<BeatmapModel>> GetBeatmap(int beatmapId)
    {
        Option<BeatmapModel> beatmap = await _dbContext.GetBeatmap(beatmapId);
        if (beatmap.IsSome()) return beatmap.Value();
        
        _logger.LogInformation("Missing beatmap: {BeatmapId}", beatmapId);
        ApiResult<BeatmapExtended> apiResponse = await _osuApiClient.GetBeatmapAsync(beatmapId);
        if (apiResponse.IsFailure)
        {
            switch (apiResponse.Error.Type)
            {
                // This endpoint can't realistically hit anything outside of BeatmapNotFound
                case ApiErrorType.BeatmapNotFound:
                    _logger.LogInformation("Failed to find a beatmap");
                    return new BeatmapNotFound(beatmapId);
                case ApiErrorType.Unknown:
                    _logger.LogInformation("Unknown osu! api error, {ApiErrorDetails}", apiResponse.Error.Message);
                    return new UpstreamError(apiResponse.Error.Message ?? "");
                default:
                    throw new UnreachableException();
            }
        }
        if (apiResponse.Value is null)
        {
            return new BeatmapNotFound(beatmapId);
        }

        var onlineBeatmap = apiResponse.Value!.ToModel();
        return onlineBeatmap;
    }

    public async Task<RequestServiceResult<UserModel>> GetUser(int userId)
    {
        Option<UserModel> user = await _dbContext.GetUser(userId);
        if (user.IsSome()) return user.Value();
        
        _logger.LogInformation("Missing user: {UserId}", userId);
        return new UserNotFound(userId);
    }
    
    public async Task<RequestServiceResult<UserModel>> GetUser(string userName)
    {
        Option<UserModel> user = await _dbContext.GetUserByName(userName);
        if (user.IsSome()) return user.Value();
        
        _logger.LogInformation("Missing user: {UserId}", userName);
        return new UserNotFound(null);
    }

    private async Task<OneOf<Success, Error>> InitializeRequest(UserModel? sourceUser, UserModel destinationUser, BeatmapModel beatmap, RequestSource source = RequestSource.Website)
    {
        var request = new RequestModel
        {
            Beatmap = beatmap,
            RequestedFrom = sourceUser,
            RequestedTo = destinationUser,
            Source = source,
        };
        await _dbContext.AddRequest(request);
        var notificationBody = new ReceivedRequestResponse
        {
            Id = request.Id,
            Beatmap = request.Beatmap.IntoDTO(),
            From = request.RequestedFrom?.IntoDTO(),
            Source = request.Source
        };

        await _notification.NotifyUserAsync(destinationUser.Username, notificationBody);
        return new Success();
    }

    public async Task<RequestServiceResult<Ok>> CreateRequest(int? sourceId, int destinationId, int beatmapId,
        RequestSource source = RequestSource.Website)
    {
        var sourceUser = sourceId is null ? null : await GetUser(sourceId.Value).ThrowIfNotFound();
        var destinationUser = await GetUser(destinationId).ThrowIfNotFound();
        var beatmap = await GetBeatmap(beatmapId).ThrowIfNotFound();
        
        var result = await InitializeRequest(sourceUser, destinationUser, beatmap, source);
        return result.Match<RequestServiceResult<Ok>>(ok => new Ok(), err => new InternalError());
    }
    public async Task<RequestServiceResult<Ok>> CreateRequest(int? sourceId, string destinationName, int beatmapId,
        RequestSource source = RequestSource.Website)
    {
        var sourceUser = sourceId is null ? null : await GetUser(sourceId.Value).ThrowIfNotFound();
        var destinationUser = await GetUser(destinationName).ThrowIfNotFound();
        var beatmap = await GetBeatmap(beatmapId).ThrowIfNotFound();
        
        var result = await InitializeRequest(sourceUser, destinationUser, beatmap, source);
        return result.Match<RequestServiceResult<Ok>>(ok => new Ok(), err => new InternalError());
    }
}