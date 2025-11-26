using osu.NET;
using osuRequestor.Apis.OsuApi;
using osuRequestor.Apis.OsuApi.Interfaces;
using osuRequestor.Services;
using osuRequestor.SignalR;

namespace osuRequestor.Setup;

public static class ServiceSetup
{
   public static void AddServiceSetup(this IServiceCollection services)
   {
      services.AddHttpContextAccessor();
      services.AddHttpClient<OsuApiProvider>();
      services.AddSignalR();
      services.AddTransient<RequestService>();
      
      services.AddScoped<IRequestNotificationService, RequestNotificationService>();
      services.AddScoped<IUserContext, HttpUserContext>();
      services.AddScoped<OsuDatabaseAccessTokenProvider>();
      services.AddScoped<OsuApiClient>((serviceProvider) =>
      {
         var provider = serviceProvider.GetRequiredService<OsuDatabaseAccessTokenProvider>();
         var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
         return new OsuApiClient(provider, 
            loggerFactory.CreateLogger("UserTokenOsuApiClient") as ILogger<OsuApiClient>);
      });
      services.AddSingleton<IOsuApiProvider, OsuApiProvider>();
   }
}