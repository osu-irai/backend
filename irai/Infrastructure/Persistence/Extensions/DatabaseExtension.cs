using irai.Common.Exceptions;
using irai.Common.Extensions;
using irai.Common.Models;
using irai.Features.Authentication.Persistence;
using irai.Features.Requests.Persistence;
using irai.Features.Users.Persistence;
using Microsoft.EntityFrameworkCore;

namespace irai.Infrastructure.Persistence.Extensions;

/// <summary>
/// Extension methods for DB context. Added for DRY
/// </summary>
public static class DatabaseExtension
{
    /// <summary>
    /// Get user by osu! user ID
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="id">User ID</param>
    /// <returns>Optional UserModel</returns>
    public static async Task<Option<UserModel>> GetUser(this DatabaseContext dbContext, int? id)
    {
        return await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id)
                              .IntoOptionAsync();
    }

    /// <summary>
    /// Get user by osu! username
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="name">Username as per osu!</param>
    /// <returns>Optional UserModel</returns>
    public static async Task<Option<UserModel>> GetUserByName(this DatabaseContext dbContext, string name)
    {
        return await dbContext.Users
                              .FirstOrDefaultAsync(u => u.Username.ToLower() == name.ToLower())
                              .IntoOptionAsync();
    }

    /// <summary>
    /// Get user by their Identity claim, including Token data
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="claim">Identity claim, should be equal to user ID</param>
    /// <returns>Optional UserModel. If some, includes token information</returns>
    public static async Task<Option<UserModel>> GetUserByClaim(this DatabaseContext dbContext, int claim)
    {
        return await dbContext.Users
                              .Where(s => s.Id == claim)
                              .Include(s => s.Token)
                              .FirstOrDefaultAsync()
                              .IntoOptionAsync();
    }

    /// <summary>
    /// Get beatmap by <paramref name="id" />
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="id">Beatmap ID</param>
    /// <returns>
    /// Optional beatmap. Beatmaps are only stored after requests so it will be None if used on an
    /// unused beatmap
    /// </returns>
    public static async Task<Option<BeatmapModel>> GetBeatmap(this DatabaseContext dbContext, int? id)
    {
        return await dbContext.Beatmaps
                              .Include(b => b.BeatmapSet)
                              .FirstOrDefaultAsync(b => b.Id == id)
                              .IntoOptionAsync();
    }

    /// <summary>
    /// Update user's osu! oauth token in the database
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="id">User ID</param>
    /// <param name="token">New token</param>
    public static async Task UpdateToken(this DatabaseContext dbContext, int id, TokenModel token)
    {
        var tok = await dbContext.Tokens.FirstOrDefaultAsync(t => t.UserId == id);
        if (tok is null)
        {
            throw new UnauthorizedException();
        }

        tok.AccessToken = token.AccessToken;
        tok.Expires     = token.Expires;
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Store a beatmap request. Re-requesting the same map to the same player from the same sender
    /// only bumps the date instead of adding a row. Returns the stored row (existing or new).
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="request">Newly added request</param>
    public static async Task<RecordStatus<RequestModel, int>> AddRequest(
        this DatabaseContext dbContext, RequestModel request)
    {
        var requestedFromId = request.RequestedFrom?.Id;
        var existing = await dbContext.Requests
                                      .Include(req => req.Beatmap)
                                      .ThenInclude(beatmap => beatmap.BeatmapSet)
                                      .Include(req => req.RequestedTo)
                                      .Include(req => req.RequestedFrom)
                                      .FirstOrDefaultAsync(req =>
                                                               !req.IsDeleted &&
                                                               req.Beatmap.Id == request.Beatmap.Id &&
                                                               req.RequestedTo.Id == request.RequestedTo.Id &&
                                                               ((requestedFromId == null &&
                                                                 req.RequestedFrom == null) ||
                                                                (requestedFromId != null && req.RequestedFrom != null &&
                                                                 req.RequestedFrom.Id == requestedFromId)));
        if (existing is not null)
        {
            existing.Date = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
            return new Updated<int>(existing.Id);
        }

        request.Date = DateTime.UtcNow;
        var bms = await dbContext.BeatmapSets.FirstOrDefaultAsync(bms => bms.Id == request.Beatmap.BeatmapSet.Id);
        if (bms is not null)
        {
            request.Beatmap.BeatmapSet = bms;
        }

        dbContext.Requests.Add(request);
        await dbContext.SaveChangesAsync();
        return new Created<RequestModel>(request);
    }
}
