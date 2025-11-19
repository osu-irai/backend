using Microsoft.EntityFrameworkCore;
using osuRequestor.Data;
using osuRequestor.DTO.General;
using osuRequestor.DTO.Responses;
using osuRequestor.Extensions;
using osuRequestor.Models;

namespace osuRequestor.Persistence;

public static class DatabaseExtension
{
    public static async Task<Option<UserModel>> GetUser(this DatabaseContext dbContext, int? id)
    {
        return await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id).IntoOptionAsync();
    }

    public static async Task<Option<UserModel>> GetUserByName(this DatabaseContext dbContext, string name)
    {
        return await dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == name).IntoOptionAsync();
    }

    public static async Task<Option<UserModel>> GetUserByClaim(this DatabaseContext dbContext, int claim)
    {
        return await dbContext.Users
            .Where(s => s.Id== claim)
            .Include(s => s.Token)
            .FirstOrDefaultAsync().IntoOptionAsync();
    }

    public static async Task<List<UserModel>> QueryUsers(this DatabaseContext dbContext, string? query)
    {
        var queryString = query ?? String.Empty;
        return await dbContext
            .Users
            .AsNoTracking()
            .Where(u => u.Username.ToLower().StartsWith(queryString.ToLower()))
            .OrderBy(u => u.Username)
            .Take(10)
            .ToListAsync();

    }

    public static async Task<Option<BeatmapModel>> GetBeatmap(this DatabaseContext dbContext, int? id)
    {
        return await dbContext.Beatmaps
            .Include(b => b.BeatmapSet)
            .FirstOrDefaultAsync(b => b.Id == id)
            .IntoOptionAsync();
    }

    public static async Task AddUser(this DatabaseContext dbContext, UserModel user)
    {
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
    }

    public static async Task AddBeatmap(this DatabaseContext dbContext, BeatmapModel map)
    {
        dbContext.Beatmaps.Add(map);
        await dbContext.SaveChangesAsync();
    }

    public static async Task UpdateToken(this DatabaseContext dbContext, int id, TokenModel token)
    {
        var tok = await dbContext.Tokens.FirstAsync(t => t.UserId == id);
        tok.AccessToken = token.AccessToken;
        tok.Expires= token.Expires;
        await dbContext.SaveChangesAsync();
    }

    public static async Task<Option<RequestModel>> GetRequest(this DatabaseContext dbContext, int id)
    {
        return await dbContext.Requests.AsNoTracking()
            .Include(r => r.RequestedFrom)
            .Include(r => r.RequestedTo)
            .Include(r => r.Beatmap)
            .FirstOrDefaultAsync(r => r.Id == id)
            .IntoOptionAsync();
    }

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
                From = x.RequestedFrom != null ? new UserDTO
                {
                    Id = x.RequestedFrom.Id,
                    Username = x.RequestedFrom.Username,
                    AvatarUrl = x.RequestedFrom.AvatarUrl
                } : null,
                Source = x.Source,
            })
            .Take(50).ToListAsync();
    }

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
            if (bms is not null)
            {
                request.Beatmap.BeatmapSet = bms;
            }
            dbContext.Requests.Add(request);
        }
        await dbContext.SaveChangesAsync();
    }

    public static async Task DeleteRequest(this DatabaseContext dbContext, int id)
    {
        var req = await dbContext.Requests.FirstOrDefaultAsync(r => r.Id == id);
        if (req is not null)
        {
            req.IsDeleted = true;
        }
        await dbContext.SaveChangesAsync();
    }
}
