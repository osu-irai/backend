using Hangfire.Dashboard;

namespace irai.Infrastructure.Hangfire;

/// <summary>
/// Only logged-in users may open /hangfire. There is no admin role, so any authenticated user is accepted.
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    #region IDashboardAuthorizationFilter Members

    public bool Authorize(DashboardContext context)
    {
        return context.GetHttpContext()
                      .User.Identity?.IsAuthenticated == true;
    }

    #endregion
}
