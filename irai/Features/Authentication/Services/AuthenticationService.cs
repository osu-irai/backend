using osuRequestor.Infrastructure.ExternalServices.OsuApi.Models;
using osuRequestor.Infrastructure.Persistence;
using osuRequestor.Infrastructure.Persistence.Models;

namespace osuRequestor.Features.Authentication.Services;

public class AuthenticationService(
    DatabaseContext databaseContext,
    ILogger<AuthenticationService> logger) : IAuthenticationService
{
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
            existingTokens.AccessToken  = accessToken;
            existingTokens.RefreshToken = refreshToken;
            existingTokens.Expires      = tokenExpiration;

            databaseContext.Tokens.Update(existingTokens);
        }
        else
        {
            await databaseContext.Tokens.AddAsync(new TokenModel
            {
                UserId       = user.Id,
                AccessToken  = accessToken,
                RefreshToken = refreshToken,
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
                AccessToken  = accessToken,
                RefreshToken = refreshToken,
                TwitchId     = twitchId
            });
        }
        else
        {
            twitchUserModel.Username = username;
        }

        var settings = await databaseContext.Settings.FindAsync(osuId);
        if (settings != null)
        {
            settings.EnableTwitch = true;
        }

        await databaseContext.SaveChangesAsync();
    }
}
