using ChiffadicErrors;
using irai.Common.Constants;
using irai.Common.Extensions;
using irai.Features.Users.Persistence;
using irai.Infrastructure.ExternalServices.OsuApi.Interfaces;
using irai.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;

namespace irai.Features.Authentication.Services;

public class FriendService(
    DatabaseContext context,
    IOsuApiProvider osuApiProvider,
    IDataProtectionProvider protectionProvider,
    ILogger<FriendService> logger) : IFriendService
{
    private readonly IDataProtector _protector = protectionProvider.CreateProtector(TokenProtection.PURPOSE);

    #region IFriendService Members

    public async Task GetFriends(int userId)
    {
        var token = await context.Tokens.FindAsync(userId);
        if (token is null)
        {
            logger.LogWarning("Skipping friend sync for {UserId}, no stored token", userId);
            return;
        }

        if (this._protector.UnprotectToken(token.AccessToken) is not Ok<string> { Value: var accessToken })
        {
            logger.LogWarning("Skipping friend sync for {UserId}, stored token cannot be decrypted", userId);
            return;
        }

        var friends = await osuApiProvider.GetFriends(accessToken);

        var friendIds = friends.Select(f => new FriendModel
        {
            UserId   = userId,
            FriendId = f.Id
        });

        await context.Friends.AddRangeAsync(friendIds);
        await context.SaveChangesAsync();
    }

    #endregion
}
