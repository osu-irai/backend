using Microsoft.EntityFrameworkCore;
using osuRequestor.Data;
using osuRequestor.DTO.General;
using osuRequestor.DTO.Responses;
using osuRequestor.Extensions;
using osuRequestor.Models;

namespace osuRequestor.Persistence;

/// <summary>
///     Extension methods for DB context. Added for DRY
/// </summary>
public static class DatabaseExtension
{
    /// <summary>
    ///     Get user by osu! user ID
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="id">User ID</param>
    /// <returns>Optional UserModel</returns>
    public static async Task<Option<UserModel>> GetUser(this DatabaseContext dbContext, int? id)
    {
        return await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id).IntoOptionAsync();
    }

    /// <summary>
    ///     Get user by osu! username
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="name">Username as per osu!</param>
    /// <returns>Optional UserModel</returns>
    public static async Task<Option<UserModel>> GetUserByName(this DatabaseContext dbContext, string name)
    {
        return await dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == name).IntoOptionAsync();
    }

    /// <summary>
    ///     Get user by their Identity claim, including Token data
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="claim">Identity claim, should be equal to user ID</param>
    /// <returns>Optional UserModel. If some, includes token information</returns>
    public static async Task<Option<UserModel>> GetUserByClaim(this DatabaseContext dbContext, int claim)
    {
        return await dbContext.Users
            .Where(s => s.Id == claim)
            .Include(s => s.Token)
            .FirstOrDefaultAsync().IntoOptionAsync();
    }

    /// <summary>
    ///     Get list of users whose usernames start with <paramref name="query" />
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="query">Query string</param>
    /// <returns></returns>
    public static async Task<List<UserModel>> QueryUsers(this DatabaseContext dbContext, string? query)
    {
        var queryString = query ?? string.Empty;
        return await dbContext
            .Users
            .AsNoTracking()
            .Where(u => u.Username.ToLower().StartsWith(queryString.ToLower()))
            .OrderBy(u => u.Username)
            .Take(10)
            .ToListAsync();
    }

    /// <summary>
    ///     Get beatmap by <paramref name="id" />
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="id">Beatmap ID</param>
    /// <returns>
    ///     Optional beatmap. Beatmaps are only stored after requests so it will be None if used on an
    ///     unused beatmap
    /// </returns>
    public static async Task<Option<BeatmapModel>> GetBeatmap(this DatabaseContext dbContext, int? id)
    {
        return await dbContext.Beatmaps
            .Include(b => b.BeatmapSet)
            .FirstOrDefaultAsync(b => b.Id == id)
            .IntoOptionAsync();
    }

    /// <summary>
    ///     Update user's osu! oauth token in the database
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="id">User ID</param>
    /// <param name="token">New token</param>
    public static async Task UpdateToken(this DatabaseContext dbContext, int id, TokenModel token)
    {
        var tok = await dbContext.Tokens.FirstAsync(t => t.UserId == id);
        tok.AccessToken = token.AccessToken;
        tok.Expires = token.Expires;
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    ///     Get all requests made to user
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="id">User ID</param>
    /// <returns>List of all requests made to user</returns>
    public static async Task<List<ReceivedRequestResponse>> GetRequestsToUser(this DatabaseContext dbContext, int id)
    {
        return await dbContext
            .Requests
            // Tracking introduces unnecessary overhead for read-only ops
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
                    BeatmapId = x.Beatmap.Id,
                    BeatmapsetId = x.Beatmap.BeatmapSet.Id,
                    Artist = x.Beatmap.BeatmapSet.Artist,
                    Title = x.Beatmap.BeatmapSet.Title,
                    Difficulty = x.Beatmap.Version,
                    Stars = x.Beatmap.StarRating
                },
                From = x.RequestedFrom != null
                    ? new UserDTO
                    {
                        Id = x.RequestedFrom.Id,
                        Username = x.RequestedFrom.Username,
                        AvatarUrl = x.RequestedFrom.AvatarUrl
                    }
                    : null,
                Source = x.Source
            })
            .Take(50).ToListAsync();
    }

    /// <summary>
    ///     Store a beatmap request
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="request">Newly added request</param>
    public static async Task AddRequest(this DatabaseContext dbContext, RequestModel request)
    {
        var existing = await dbContext.Requests.FirstOrDefaultAsync(req =>
            req.RequestedFrom.Id == request.RequestedFrom.Id
            && req.Beatmap.Id == request.Beatmap.Id
            && req.RequestedTo.Id == request.RequestedTo.Id);
        if (existing is not null)
        {
            existing.Date = DateTime.UtcNow;
        }
        else
        {
            request.Date = DateTime.UtcNow;
            var bms = await dbContext.BeatmapSets.FirstOrDefaultAsync(bms => bms.Id == request.Beatmap.BeatmapSet.Id);
            if (bms is not null) request.Beatmap.BeatmapSet = bms;

            dbContext.Requests.Add(request);
        }

        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    ///     Delete a request by request ID
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="id">Request ID</param>
    public static async Task DeleteRequest(this DatabaseContext dbContext, int id)
    {
        var req = await dbContext.Requests.FirstOrDefaultAsync(r => r.Id == id);
        if (req is not null) req.IsDeleted = true;

        await dbContext.SaveChangesAsync();
    }
}