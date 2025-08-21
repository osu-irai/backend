using Microsoft.EntityFrameworkCore;
using osuRequestor.Data;
using osuRequestor.DTO.General;
using osuRequestor.DTO.Responses;
using osuRequestor.Models;

namespace osuRequestor.Persistence;

public class Repository
{
    private readonly DatabaseContext _dbContext;

    public Repository(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserModel?> GetUser(int? id)
    {
        return await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<UserModel?> GetUserByClaim(int claim)
    {
        UserModel? source = await _dbContext.Tokens
            .Where(s => s.UserId == claim)
            .Select(s => s.User)
            .FirstOrDefaultAsync();
        return source;
    }

    public async Task<List<UserModel>> QueryUsers(string? query)
    {
        return await _dbContext
            .Users
            .AsNoTracking()
            .Where(u => u.Username.StartsWith(query ?? String.Empty))
            .OrderBy(u => u.Username)
            .Take(10)
            .ToListAsync();

    }

    public async Task<BeatmapModel?> GetBeatmap(int? id)
    {
        return await _dbContext.Beatmaps.AsNoTracking().Include(b => b.BeatmapSet).FirstOrDefaultAsync(b => b.Id == id);
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

    public async Task<RequestModel?> GetRequest(int id)
    {
        return await _dbContext.Requests.AsNoTracking().Include(r => r.RequestedFrom).Include(r => r.RequestedTo)
            .Include(r => r.Beatmap).FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<ReceivedRequestResponse>> GetRequestsToUser(int id)
    {
        return await _dbContext
            .Requests
            // Tracking introduces unnecessary overhead for read-only ops
            .AsNoTracking()
            .Include(requestModel => requestModel.Beatmap)
            .Include(requestModel => requestModel.RequestedTo)
            .Where(req => req.RequestedTo.Id == id || !req.IsDeleted)
            .OrderByDescending(i => i.Id)
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
                From = new UserDTO
                {
                    Id = x.RequestedFrom.Id,
                    Username = x.RequestedFrom.Username,
                    AvatarUrl = x.RequestedFrom.AvatarUrl
                }
            })
            .Take(50).ToListAsync();
    }

    public async Task AddRequest(RequestModel request)
    {
        _dbContext.Requests.Add(request);
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