using MassTransit;

namespace osuRequestor.Services.MessageQueue.Contracts;

[EntityName("twitch-settings-change-contract")]
public class TwitchSettingsChangeContract
{
    public string TwitchUserId { get; set; }
    public int OsuId { get; set; }
    
    public bool IsEnabled { get; set; }
}