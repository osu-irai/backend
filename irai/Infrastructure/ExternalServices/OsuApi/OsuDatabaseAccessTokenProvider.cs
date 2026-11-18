using irai.Common.Constants;
using irai.Common.Exceptions;
using irai.Common.Extensions;
using irai.Features.Authentication.Persistence;
using irai.Infrastructure.ExternalServices.OsuApi.Interfaces;
using irai.Infrastructure.Persistence;
using irai.Infrastructure.Persistence.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using osu.NET.Authorization;

namespace irai.Infrastructure.ExternalServices.OsuApi;

public class OsuDatabaseAccessTokenProvider(
    DatabaseContext dbContext,
    IHttpContextAccessor contextAccessor,
    IOsuApiProvider osuApiProvider,
    IDataProtectionProvider protectionProvider,
    ILogger<OsuDatabaseAccessTokenProvider> logger)
    : IOsuAccessTokenProvider
{
    private static readonly SemaphoreSlim RefreshLock = new SemaphoreSlim(1, 1);
    private readonly IDataProtector _protector = protectionProvider.CreateProtector(TokenProtection.PURPOSE);

    #region IOsuAccessTokenProvider Members

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Querying user info");
        var ctx = contextAccessor.HttpContext;
        if (ctx is null)
        {
            // This should only be hit for RabbitMQ
            return await osuApiProvider.GetUserlessTokenAsync()
                   ?? throw new UnauthorizedException("Userless token unavailable");
        }

        var osuAuthResult = await ctx.AuthenticateAsync("InternalCookies");
        if (!osuAuthResult.Succeeded)
        {
            logger.LogInformation("osu auth failed: {Failure}", osuAuthResult.Failure?.Message);
            throw new UnauthorizedException();
        }

        var identity = osuAuthResult.Principal?.Identity;
        if (identity is null)
        {
            throw new ArgumentException("Invalid user identity");
        }

        var userId = identity.Name ?? throw new ArgumentException("Invalid user identity");

        var id   = int.Parse(userId);
        var user = await dbContext.GetUserByClaim(id);
        logger.LogInformation("Found valid user by claim id {Id}", id);
        var token = user.Unwrap()
                        .Token;
        if (this._protector.UnprotectToken(token.AccessToken) is not OkVariant<string> { Value: var accessToken } ||
            this._protector.UnprotectToken(token.RefreshToken) is not OkVariant<string> { Value: var refreshToken })
        {
            // Stored before at-rest encryption shipped — force re-login instead of 500ing forever.
            throw new UnauthorizedException();
        }

        logger.LogInformation("Current time is {Time}, Token for {User} expires at {Expires}", DateTime.UtcNow, id,
                              token?.Expires);
        if (DateTime.UtcNow > token?.Expires)
        {
            await RefreshLock.WaitAsync(cancellationToken);
            try
            {
                logger.LogInformation("Refreshing token for {User}", id);
                var newToken = await osuApiProvider.RefreshToken(refreshToken, accessToken);
                if (newToken is null)
                {
                    // Refresh token revoked or expired — force re-login instead of 500ing forever.
                    throw new UnauthorizedException();
                }

                await dbContext.UpdateToken(id, new TokenModel
                {
                    UserId       = id,
                    AccessToken  = this._protector.Protect(newToken.AccessToken),
                    RefreshToken = this._protector.Protect(refreshToken),
                    Expires      = DateTime.UtcNow.AddSeconds(newToken.ExpiresIn),
                    User         = user.Unwrap()
                });
                return newToken.AccessToken;
            }
            finally
            {
                RefreshLock.Release();
            }
        }

        return accessToken;
    }

    #endregion
}
