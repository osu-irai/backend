using static ChiffadicErrors.Result;
using System.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using osu.NET;
using osuRequestor.Common.Extensions;
using osuRequestor.Common.Models;
using osuRequestor.Features.Requests.Mappings;
using osuRequestor.Features.Requests.Models;
using osuRequestor.Infrastructure.Messaging;
using osuRequestor.Infrastructure.Notifications;
using osuRequestor.Infrastructure.Persistence;
using osuRequestor.Infrastructure.Persistence.Extensions;
using osuRequestor.Infrastructure.Persistence.Models;

namespace osuRequestor.Features.Requests.Services;

public class RequestService(
    ILogger<RequestService> logger,
    DatabaseContext dbContext,
    OsuApiClient osuApiClient,
    IRequestNotificationService notification,
    IMessageBusService messageBus) : IRequestService
{
    public async Task<Result<BeatmapModel, RequestServiceError>> GetBeatmap(int beatmapId)
    {
        var beatmap = await dbContext.GetBeatmap(beatmapId);
        if (beatmap is Some<BeatmapModel> some)
        {
            return Ok(some.Value);
        }

        logger.LogInformation("Missing beatmap: {BeatmapId}", beatmapId);
        var apiResponse = await osuApiClient.GetBeatmapAsync(beatmapId);
        if (apiResponse.IsFailure)
        {
            switch (apiResponse.Error.Type)
            {
                // This endpoint can't realistically hit anything outside BeatmapNotFound
                case ApiErrorType.BeatmapNotFound:
                    logger.LogInformation("Failed to find a beatmap");
                    return Error<RequestServiceError>(new BeatmapNotFound(beatmapId));
                case ApiErrorType.Unknown:
                    logger.LogInformation("Unknown osu! api error, {ApiErrorDetails}", apiResponse.Error.Message);
                    return Error<RequestServiceError>(new UpstreamError(apiResponse.Error.Message ?? ""));
                default:
                    throw new UnreachableException();
            }
        }

        if (apiResponse.Value is null)
        {
            return Error<RequestServiceError>(new BeatmapNotFound(beatmapId));
        }

        var onlineBeatmap = apiResponse.Value!.ToModel();
        return Ok(onlineBeatmap);
    }

    public async Task<Result<UserModel, RequestServiceError>> GetUser(int userId)
    {
        logger.LogInformation("Looking up user: {UserId}", userId);
        var user = await dbContext.GetUser(userId);
        
        return user switch
        {
            Some<UserModel> some => Ok(some.Value),
            None _               => Error<RequestServiceError>(new UserNotFound(userId)),
            null                 => throw new ArgumentOutOfRangeException(nameof(userId))
        };
    }

    public async Task<Result<UserModel, RequestServiceError>> GetUser(string userName)
    {
        var user = await dbContext.GetUserByName(userName);

        return user switch
        {
            Some<UserModel> some => Ok(some.Value),
            None _               => Error<RequestServiceError>(new UserNotFound(null)),
            null                 => throw new ArgumentOutOfRangeException(nameof(user))
        };
    }

    public async Task<List<ReceivedRequestResponse>> GetRequestsToUser(int id)
    {
        return await dbContext.Requests
                              .AsNoTracking()
                              .Include(requestModel => requestModel.Beatmap)
                              .Include(requestModel => requestModel.RequestedTo)
                              .Where(req => req.RequestedTo.Id == id && !req.IsDeleted)
                              .OrderByDescending(req => req.Date)
                              .Select(x => new ReceivedRequestResponse
                              {
                                  Id = x.Id,
                                  Beatmap = new BeatmapDTO
                                  {
                                      BeatmapId    = x.Beatmap.Id,
                                      BeatmapsetId = x.Beatmap.BeatmapSet.Id,
                                      Artist       = x.Beatmap.BeatmapSet.Artist,
                                      Title        = x.Beatmap.BeatmapSet.Title,
                                      Difficulty   = x.Beatmap.Version,
                                      Stars        = x.Beatmap.StarRating
                                  },
                                  From = x.RequestedFrom != null
                                             ? new UserDTO
                                             {
                                                 Id        = x.RequestedFrom.Id,
                                                 Username  = x.RequestedFrom.Username,
                                                 AvatarUrl = x.RequestedFrom.AvatarUrl
                                             }
                                             : null,
                                  Source = x.Source
                              })
                              .Take(50)
                              .ToListAsync();
    }

    public async Task DeleteRequest(int id)
    {
        var request = await dbContext.Requests.FirstOrDefaultAsync(r => r.Id == id);
        if (request is not null)
        {
            request.IsDeleted = true;
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task<int> InitializeRequest(UserModel? sourceUser, UserModel destinationUser,
                                              BeatmapModel beatmap,
                                              RequestSource source = RequestSource.Website)
    {
        var request = new RequestModel
        {
            Beatmap       = beatmap,
            RequestedFrom = sourceUser,
            RequestedTo   = destinationUser,
            Source        = source
        };
        await dbContext.AddRequest(request);
        var notificationBody = new ReceivedRequestResponse
        {
            Id      = request.Id,
            Beatmap = request.Beatmap.IntoDTO(),
            From    = request.RequestedFrom?.IntoDTO(),
            Source  = request.Source
        };

        await notification.NotifyUserAsync(destinationUser.Username, notificationBody);
        await messageBus.SubmitRequestAsync(destinationUser.Username, notificationBody);

        return request.Id;
    }

    /// <summary>
    /// </summary>
    /// <param name="sourceId">
    /// Request source user ID.
    /// Since creation logic is shared between sourceful (frontend, IRC) and sourceless (Twitch) paths,
    /// it is valid for sourceId to be null
    /// </param>
    /// <param name="destinationId"></param>
    /// <param name="beatmapId"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    public async Task<Result<int, RequestServiceError>> CreateRequest(int? sourceId, int destinationId, int beatmapId,
                                                                      RequestSource source = RequestSource.Website)
    {
        var sourceUser = sourceId is null
                             ? null
                             : await GetUser(sourceId.Value)
                                   .ThrowIfRequestError();
        var destinationUser = await GetUser(destinationId)
                                  .ThrowIfRequestError();
        var beatmap = await GetBeatmap(beatmapId)
                          .ThrowIfRequestError();

        var result = await InitializeRequest(sourceUser, destinationUser, beatmap, source);

        return new OkVariant<int>(result);
    }

    public async Task<Result<int, RequestServiceError>> CreateRequest(int? sourceId, string destinationName,
                                                                      int beatmapId,
                                                                      RequestSource source = RequestSource.Website)
    {
        var sourceUser = sourceId is null
                             ? null
                             : await GetUser(sourceId.Value)
                                   .ThrowIfRequestError();
        var destinationUser = await GetUser(destinationName)
                                  .ThrowIfRequestError();
        var beatmap = await GetBeatmap(beatmapId)
                          .ThrowIfRequestError();

        var result = await InitializeRequest(sourceUser, destinationUser, beatmap, source);

        return new OkVariant<int>(result);
    }
}
