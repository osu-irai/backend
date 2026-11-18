using ChiffadicErrors;
using irai.Common.Extensions;
using irai.Common.Models;
using irai.Features.Requests.Mappings;
using irai.Features.Requests.Models;
using irai.Features.Requests.Persistence;
using irai.Features.Users.Persistence;
using irai.Infrastructure.Messaging;
using irai.Infrastructure.Notifications;
using irai.Infrastructure.Persistence;
using irai.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;
using osu.NET;
using static ChiffadicErrors.Result;

namespace irai.Features.Requests.Services;

public class RequestService(
    ILogger<RequestService> logger,
    DatabaseContext dbContext,
    OsuApiClient osuApiClient,
    IRequestNotificationService notification,
    IMessageBusService messageBus) : IRequestService
{
    #region IRequestService Members

    public async Task<Result<BeatmapModel, RequestServiceError>> GetBeatmap(int beatmapId)
    {
        var beatmap = await dbContext.GetBeatmap(beatmapId);
        if (beatmap is Some<BeatmapModel> some)
        {
            return Ok(some.Value);
        }

        logger.LogInformation("Missing beatmap: {BeatmapId}", beatmapId);
        return await FetchBeatmapAsync(beatmapId);
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

    public async Task<List<SentRequestResponse>> GetRequestsByUser(int id)
    {
        return await dbContext.Requests
                              .AsNoTracking()
                              .Where(req => req.RequestedFrom!.Id == id && !req.IsDeleted)
                              .OrderByDescending(req => req.Date)
                              .Select(x => new SentRequestResponse
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
                                  To = new UserDTO
                                  {
                                      Id        = x.RequestedTo.Id,
                                      Username  = x.RequestedTo.Username,
                                      AvatarUrl = x.RequestedTo.AvatarUrl
                                  }
                              })
                              .Take(50)
                              .ToListAsync();
    }

    public async Task DeleteRequest(int id, int callerId)
    {
        var request = await dbContext.Requests.FirstOrDefaultAsync(r => r.Id == id &&
                                                                        (r.RequestedTo.Id    == callerId ||
                                                                         r.RequestedFrom!.Id == callerId));
        request?.IsDeleted = true;

        await dbContext.SaveChangesAsync();
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

        return new Ok<int>(result);
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

        return new Ok<int>(result);
    }

    #endregion

    private async Task<Result<BeatmapModel, RequestServiceError>> FetchBeatmapAsync(int beatmapId)
    {
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
                    return Error<RequestServiceError>(new InternalError("Unknown application error"));
            }
        }

        if (apiResponse.Value is null)
        {
            return Error<RequestServiceError>(new BeatmapNotFound(beatmapId));
        }

        var onlineBeatmap = apiResponse.Value!.ToModel();
        return Ok(onlineBeatmap);
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
        var stored = await dbContext.AddRequest(request);

        if (stored is Updated<int> updated)
        {
            return updated.Value;
        }

        var created = (stored.Value as Created<RequestModel>)!.Value;

        var notificationBody = new ReceivedRequestResponse
        {
            Id      = created.Id,
            Beatmap = created.Beatmap.IntoDTO(),
            From    = created.RequestedFrom?.IntoDTO(),
            Source  = created.Source
        };

        await notification.NotifyUserAsync(destinationUser.Username, notificationBody);
        try
        {
            await messageBus.SubmitRequestAsync(destinationUser.Username, notificationBody);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to publish request {RequestId} to the message bus", created.Id);
        }

        return created.Id;
    }
}
