using System.Diagnostics;
using OneOf;
using OneOf.Types;
using osu.NET;
using osuRequestor.Data;
using osuRequestor.DTO.Responses;
using osuRequestor.Exceptions;
using osuRequestor.Extensions;
using osuRequestor.Models;
using osuRequestor.Persistence;
using osuRequestor.SignalR;

namespace osuRequestor.Services;

public class RequestService(
    ILogger<RequestService> logger,
    DatabaseContext dbContext,
    OsuApiClient osuApiClient,
    IRequestNotificationService notification)
{
    public async Task<RequestServiceResult<BeatmapModel>> GetBeatmap(int beatmapId)
    {
        var beatmap = await dbContext.GetBeatmap(beatmapId);
        if (beatmap.IsSome()) return beatmap.Value();

        logger.LogInformation("Missing beatmap: {BeatmapId}", beatmapId);
        var apiResponse = await osuApiClient.GetBeatmapAsync(beatmapId);
        if (apiResponse.IsFailure)
            switch (apiResponse.Error.Type)
            {
                // This endpoint can't realistically hit anything outside BeatmapNotFound
                case ApiErrorType.BeatmapNotFound:
                    logger.LogInformation("Failed to find a beatmap");
                    return new BeatmapNotFound(beatmapId);
                case ApiErrorType.Unknown:
                    logger.LogInformation("Unknown osu! api error, {ApiErrorDetails}", apiResponse.Error.Message);
                    return new UpstreamError(apiResponse.Error.Message ?? "");
                default:
                    throw new UnreachableException();
            }

        if (apiResponse.Value is null) return new BeatmapNotFound(beatmapId);

        var onlineBeatmap = apiResponse.Value!.ToModel();
        return onlineBeatmap;
    }

    public async Task<RequestServiceResult<UserModel>> GetUser(int userId)
    {
        var user = await dbContext.GetUser(userId);
        if (user.IsSome()) return user.Value();

        logger.LogInformation("Missing user: {UserId}", userId);
        return new UserNotFound(userId);
    }

    public async Task<RequestServiceResult<UserModel>> GetUser(string userName)
    {
        var user = await dbContext.GetUserByName(userName);
        if (user.IsSome()) return user.Value();

        logger.LogInformation("Missing user: {UserId}", userName);
        return new UserNotFound(null);
    }

    private async Task<OneOf<Success, Error>> InitializeRequest(UserModel? sourceUser, UserModel destinationUser,
        BeatmapModel beatmap, RequestSource source = RequestSource.Website)
    {
        var request = new RequestModel
        {
            Beatmap = beatmap,
            RequestedFrom = sourceUser,
            RequestedTo = destinationUser,
            Source = source
        };
        await dbContext.AddRequest(request);
        var notificationBody = new ReceivedRequestResponse
        {
            Id = request.Id,
            Beatmap = request.Beatmap.IntoDTO(),
            From = request.RequestedFrom?.IntoDTO(),
            Source = request.Source
        };

        await notification.NotifyUserAsync(destinationUser.Username, notificationBody);
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