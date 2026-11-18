using Hangfire;
using irai.Common.Constants;
using irai.Features.Authentication.Persistence;
using irai.Features.Users.Persistence;
using irai.Infrastructure.ExternalServices.OsuApi.Models;
using irai.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;

namespace irai.Features.Authentication.Services;

public class AuthenticationService(
    IBackgroundJobClient backgroundJobClient,
    IFriendService friendService,
    DatabaseContext databaseContext,
    IDataProtectionProvider protectionProvider,
    ILogger<AuthenticationService> logger) : IAuthenticationService
{
    private readonly IDataProtector _protector = protectionProvider.CreateProtector(TokenProtection.PURPOSE);

    #region IAuthenticationService Members

    public async Task<DateTime> SaveOsuLogin(User user, string accessToken, string refreshToken)
    {
        var existingUser = await databaseContext.Users.FindAsync(user.Id);
        if (existingUser is null)
        {
            logger.LogInformation("User with id {Id} not found", user.Id);
            await databaseContext.Users.AddAsync(new UserModel
            {
                Id          = user.Id,
                Username    = user.Username,
                AvatarUrl   = user.AvatarUrl,
                CountryCode = user.CountryCode,
                Settings    = new SettingsModel()
            });
            backgroundJobClient.Enqueue(() => friendService.GetFriends(user.Id));
        }
        else
        {
            logger.LogInformation("User with id {Id} found", user.Id);
            existingUser.Username    = user.Username;
            existingUser.CountryCode = user.CountryCode;

            databaseContext.Users.Update(existingUser);
        }

        var tokenExpiration = DateTime.UtcNow.AddDays(1);

        var existingTokens = await databaseContext.Tokens.FindAsync(user.Id);
        if (existingTokens is not null)
        {
            existingTokens.AccessToken  = this._protector.Protect(accessToken);
            existingTokens.RefreshToken = this._protector.Protect(refreshToken);
            existingTokens.Expires      = tokenExpiration;

            databaseContext.Tokens.Update(existingTokens);
        }
        else
        {
            await databaseContext.Tokens.AddAsync(new TokenModel
            {
                UserId       = user.Id,
                AccessToken  = this._protector.Protect(accessToken),
                RefreshToken = this._protector.Protect(refreshToken),
                Expires      = tokenExpiration
            });
        }

        await databaseContext.SaveChangesAsync();
        return tokenExpiration;
    }

    public async Task SaveTwitchLogin(int osuId, int twitchId, string username, string accessToken,
                                      string refreshToken)
    {
        var twitchUserModel = await databaseContext.Twitch.FindAsync(osuId);

        if (twitchUserModel is null)
        {
            databaseContext.Twitch.Add(new TwitchModel
            {
                UserId       = osuId,
                Username     = username,
                AccessToken  = this._protector.Protect(accessToken),
                RefreshToken = this._protector.Protect(refreshToken),
                TwitchId     = twitchId
            });
        }
        else
        {
            twitchUserModel.Username     = username;
            twitchUserModel.TwitchId     = twitchId;
            twitchUserModel.AccessToken  = this._protector.Protect(accessToken);
            twitchUserModel.RefreshToken = this._protector.Protect(refreshToken);
        }

        var settings = await databaseContext.Settings.FindAsync(osuId);
        if (settings != null)
        {
            settings.EnableTwitch = true;
        }

        await databaseContext.SaveChangesAsync();
    }

    #endregion
}
