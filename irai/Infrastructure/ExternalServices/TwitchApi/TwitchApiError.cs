namespace irai.Infrastructure.ExternalServices.TwitchApi;

public record InvalidTwitchToken;

public union TwitchApiError(InvalidTwitchToken);
