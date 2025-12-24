using MassTransit;

namespace osuRequestor.Infrastructure.Messaging.Contracts;

[EntityName("irc-settings-change-contract")]
public class IrcSettingsChangeContract
{
    public string Username { get; set; }

    public bool IsEnabled { get; set; }
}
