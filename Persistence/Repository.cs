using Microsoft.EntityFrameworkCore;
using OneOf.Monads;
using osuRequestor.Data;
using osuRequestor.DTO.General;
using osuRequestor.DTO.Responses;
using osuRequestor.Extensions;
using osuRequestor.Models;

namespace osuRequestor.Persistence;

public class Repository
{
    private readonly DatabaseContext _dbContext;
    private readonly ILogger<Repository> _logger;

    public Repository(DatabaseContext dbContext, ILogger<Repository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Option<UserModel>> GetUser(int? id)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id).IntoOptionAsync();
    }

    public async Task<Option<UserModel>> GetUserByName(string name)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == name).IntoOptionAsync();
    }

    public async Task<Option<UserModel>> GetUserByClaim(int claim)
    {
        return await _dbContext.Users
            .Where(s => s.Id== claim)
            .Include(s => s.Token)
            .FirstOrDefaultAsync().IntoOptionAsync();
    }

    public async Task<List<UserModel>> QueryUsers(string? query)
    {
        var queryString = query ?? String.Empty;
        return await _dbContext
            .Users
            .AsNoTracking()
            .Where(u => u.Username.ToLower().StartsWith(queryString.ToLower()))
            .OrderBy(u => u.Username)
            .Take(10)
            .ToListAsync();

    }

    public async Task<Option<BeatmapModel>> GetBeatmap(int? id)
    {
        return await _dbContext.Beatmaps
            .Include(b => b.BeatmapSet)
            .FirstOrDefaultAsync(b => b.Id == id)
            .IntoOptionAsync();
    }

    public async Task AddUser(UserModel user)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
    }

    public async Task AddBeatmap(BeatmapModel map)
    {
        _dbContext.Beatmaps.Add(map);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateToken(int id, TokenModel token)
    {
        var tok = await _dbContext.Tokens.FirstAsync(t => t.UserId == id);
        tok.AccessToken = token.AccessToken;
        tok.Expires= token.Expires;
        await _dbContext.SaveChangesAsync();
    }

    public async Task<Option<RequestModel>> GetRequest(int id)
    {
        return await _dbContext.Requests.AsNoTracking()
            .Include(r => r.RequestedFrom)
            .Include(r => r.RequestedTo)
            .Include(r => r.Beatmap)
            .FirstOrDefaultAsync(r => r.Id == id)
            .IntoOptionAsync();
    }

    public async Task<List<ReceivedRequestResponse>> GetRequestsToUser(int id)
    {
        return await _dbContext
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

    public async Task AddRequest(RequestModel request)
    {
        var existing = await _dbContext.Requests.FirstOrDefaultAsync(req =>
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
            var bms = await _dbContext.BeatmapSets.FirstOrDefaultAsync(bms => bms.Id == request.Beatmap.BeatmapSet.Id);
            if (bms is not null)
            {
                request.Beatmap.BeatmapSet = bms;
            }
            _dbContext.Requests.Add(request);
        }
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteRequest(int id)
    {
        var req = await _dbContext.Requests.FirstOrDefaultAsync(r => r.Id == id);
        if (req is not null)
        {
            req.IsDeleted = true;
        }
        await _dbContext.SaveChangesAsync();
    }
}