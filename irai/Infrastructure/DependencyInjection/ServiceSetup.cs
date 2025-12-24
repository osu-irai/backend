using FluentValidation;
using MassTransit;
using osu.NET;
using osuRequestor.Features.Authentication.Services;
using osuRequestor.Features.Bots.Services;
using osuRequestor.Features.Requests.Models;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using osuRequestor.Features.Requests.Services;
using osuRequestor.Features.Users.Services;
using osuRequestor.Infrastructure.ExternalServices.OsuApi;
using osuRequestor.Infrastructure.ExternalServices.OsuApi.Interfaces;
using osuRequestor.Infrastructure.ExternalServices.TwitchApi;
using osuRequestor.Infrastructure.Messaging;
using osuRequestor.Infrastructure.Notifications;

namespace osuRequestor.Infrastructure.DependencyInjection;

public static class ServiceSetup
{
    public static void AddServiceSetup(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddHttpClient<OsuApiProvider>();
        services.AddSignalR();
        services.AddTransient<IRequestService, RequestService>();
        services.AddScoped<IBotService, BotService>();
        services.AddScoped<IOwnUserService, OwnUserService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddValidatorsFromAssemblyContaining<Features.Bots.Validators.PostBaseRequestValidator>();
        services.AddFluentValidationAutoValidation();

        services.AddScoped<IRequestNotificationService, RequestNotificationService>();
        services.AddScoped<IUserContext, HttpUserContext>();
        services.AddScoped<OsuDatabaseAccessTokenProvider>();
        services.AddScoped<OsuApiClient>(serviceProvider =>
        {
            var provider      = serviceProvider.GetRequiredService<OsuDatabaseAccessTokenProvider>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            return new OsuApiClient(provider,
                                    loggerFactory.CreateLogger("UserTokenOsuApiClient") as ILogger<OsuApiClient>);
        });
        services.AddSingleton<IOsuApiProvider, OsuApiProvider>();
        services.AddSingleton<TwitchApiProvider>();
        services.AddMassTransit(cfg =>
        {
            cfg.SetKebabCaseEndpointNameFormatter();
            cfg.AddConsumer<MessageSubscriberService>();

            cfg.UsingRabbitMq((context, bus) =>
            {
                bus.UseRawJsonSerializer();
                bus.UseRawJsonDeserializer();
                bus.Host("localhost", h =>
                {
                    h.Username("irai");
                    h.Password("iraipass");
                });
                bus.Message<PostBaseRequest>(x => { x.SetEntityName("request-exchange"); });
                bus.ReceiveEndpoint("send-request-userless", e => { e.Consumer<MessageSubscriberService>(context); });
            });
        });
        services.AddScoped<IMessageBusService, MessageBusService>();
    }
}
