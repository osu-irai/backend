using osu.NET.Authorization;
using osuRequestor.Apis.OsuApi.Interfaces;
using osuRequestor.Models;
using osuRequestor.Persistence;

namespace osuRequestor.Services;

public class OsuDatabaseAccessTokenProvider : IOsuAccessTokenProvider
{
    private readonly Repository _repository;
    private readonly ILogger<OsuDatabaseAccessTokenProvider> _logger;
    private readonly IOsuApiProvider _osuApiProvider;
    private readonly IHttpContextAccessor _contextAccessor;

    public OsuDatabaseAccessTokenProvider(Repository repository, IHttpContextAccessor contextAccessor, IOsuApiProvider osuApiProvider, ILogger<OsuDatabaseAccessTokenProvider> logger)
    {
        _repository = repository;
        _contextAccessor = contextAccessor;
        _osuApiProvider = osuApiProvider;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var identity = _contextAccessor.HttpContext?.User?.Identity;
        if (identity is null)
        {
            throw new ArgumentException("Invalid user identity");
        }
        var userId = identity.Name ?? throw new ArgumentException("Invalid user identity");
        var id = int.Parse(userId);
        var user = await _repository.GetUserByClaim(id);
        var token = user?.Token;
        if (DateTime.Now > token?.Expires)
        {
            _logger.LogInformation("Refreshing token for {user}", id);
            var newToken = await _osuApiProvider.RefreshToken(token.RefreshToken, token.AccessToken);
            await _repository.UpdateToken(id, new TokenModel
            {
                UserId = id,
                AccessToken = newToken!.AccessToken,
                RefreshToken = token.RefreshToken,
                Expires = DateTime.Now.AddSeconds(newToken.ExpiresIn),
                User = user!
            });
            return newToken.AccessToken;
        }
        _logger.LogInformation("Found token for user {user}", id);
        return token!.AccessToken;
    }
}