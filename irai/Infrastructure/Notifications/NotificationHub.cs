using irai.Common.Constants;
using irai.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace irai.Infrastructure.Notifications;

[Authorize(AuthenticationSchemes = "Bearer,InternalCookies", Roles = Roles.USER_OR_PROXY)]
public class NotificationHub(
    ILogger<NotificationHub> logger) : Hub<INotificationHub>
{
    public async Task JoinUserGroup(int userId)
    {
        var name = this.Context.User?.Identity?.Name;
        if (!int.TryParse(name, out var callerId) || callerId != userId)
        {
            throw new UnauthorizedException();
        }

        logger.LogInformation("Adding {UserId}", userId);
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, $"user_{userId}");
    }

    public async Task JoinServiceGroup(string name)
    {
        if (this.Context.User?.IsInRole(Roles.PROXY) != true)
        {
            throw new UnauthorizedException();
        }

        logger.LogInformation("Adding {Name} to connected services", name);
        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, "service");
    }
}
