using FluentValidation;
using irai.Features.Authentication.Services;
using irai.Features.Bots.Services;
using irai.Features.Bots.Validators;
using irai.Features.Requests.Models;
using irai.Features.Requests.Services;
using irai.Features.Search.Services;
using irai.Features.Users.Services;
using irai.Infrastructure.Configuration;
using irai.Infrastructure.ExternalServices.OsuApi;
using irai.Infrastructure.ExternalServices.OsuApi.Interfaces;
using irai.Infrastructure.ExternalServices.TwitchApi;
using irai.Infrastructure.Messaging;
using irai.Infrastructure.Notifications;
using MassTransit;
using Microsoft.Extensions.Options;
using osu.NET;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;

namespace irai.Infrastructure.DependencyInjection;

public static class ServiceSetup
{
    public static void AddServiceSetup(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddHttpClient<OsuApiProvider>();
        services.AddSignalR();
        services.AddScoped<IRequestService, RequestService>();
        services.AddScoped<IBotService, BotService>();
        services.AddScoped<IOwnUserService, OwnUserService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        services.AddScoped<IRequestNotificationService, RequestNotificationService>();
        services.AddScoped<IUserContext, HttpUserContext>();
        services.AddScoped<OsuDatabaseAccessTokenProvider>();
        services.AddScoped<IFriendService, FriendService>();
        services.AddExternalSetup();
    }

    private static void AddExternalSetup(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<PostBaseRequestValidator>();
        services.AddFluentValidationAutoValidation();
        services.AddScoped<OsuApiClient>(serviceProvider =>
        {
            var provider      = serviceProvider.GetRequiredService<OsuDatabaseAccessTokenProvider>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            return new OsuApiClient(provider,
                                    loggerFactory.CreateLogger<OsuApiClient>());
        });
        services.AddScoped<IOsuApiProvider, OsuApiProvider>();
        services.AddScoped<TwitchApiProvider>();
        services.AddMassTransit(cfg =>
        {
            cfg.SetKebabCaseEndpointNameFormatter();
            cfg.AddConsumer<MessageSubscriberService>();

            cfg.UsingRabbitMq((context, bus) =>
            {
                var broker = context.GetRequiredService<IOptions<MessageBrokerConfig>>()
                                    .Value;
                bus.UseRawJsonSerializer();
                bus.UseRawJsonDeserializer();
                bus.Host(broker.Host, h =>
                {
                    h.Username(broker.Username);
                    h.Password(broker.Password);
                });
                bus.Message<PostBaseRequest>(x => { x.SetEntityName(broker.Exchange); });
                bus.ReceiveEndpoint("send-request-userless", e => { e.Consumer<MessageSubscriberService>(context); });
            });
        });
        services.AddScoped<IMessageBusService, MessageBusService>();
    }
}
