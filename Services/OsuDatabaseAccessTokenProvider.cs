using osu.NET.Authorization;
using osuRequestor.Apis.OsuApi.Interfaces;
using osuRequestor.Data;
using osuRequestor.Models;
using osuRequestor.Persistence;

namespace osuRequestor.Services;

public class OsuDatabaseAccessTokenProvider(
    DatabaseContext dbContext,
    IHttpContextAccessor contextAccessor,
    IOsuApiProvider osuApiProvider,
    ILogger<OsuDatabaseAccessTokenProvider> logger)
    : IOsuAccessTokenProvider
{
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Querying user info");
        var identity = contextAccessor.HttpContext?.User?.Identity;
        if (identity is null) throw new ArgumentException("Invalid user identity");
        var userId = identity.Name ?? throw new ArgumentException("Invalid user identity");
        var id = int.Parse(userId);
        var user = await dbContext.GetUserByClaim(id);
        logger.LogInformation("Found valid user by claim id {id}", id);
        var token = user.Value().Token;
        logger.LogInformation("Current time is {Time}, Token for {User} expires at {Expires}", DateTime.UtcNow, id,
            token?.Expires);
        if (DateTime.UtcNow > token?.Expires)
        {
            logger.LogInformation("Refreshing token for {user}", id);
            var newToken = await osuApiProvider.RefreshToken(token.RefreshToken, token.AccessToken);
            await dbContext.UpdateToken(id, new TokenModel
            {
                UserId = id,
                AccessToken = newToken!.AccessToken,
                RefreshToken = token.RefreshToken,
                Expires = DateTime.UtcNow.AddSeconds(newToken.ExpiresIn),
                User = user.Value()
            });
            return newToken.AccessToken;
        }

        return token!.AccessToken;
    }
}