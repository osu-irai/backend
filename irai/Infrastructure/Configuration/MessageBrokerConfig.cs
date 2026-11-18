using System.ComponentModel.DataAnnotations;

namespace irai.Infrastructure.Configuration;

public class MessageBrokerConfig
{
    public const string POSITION = "MessageBroker";

    [Required]
    public string Host { get; set; } = "localhost";

    [Required]
    public string Username { get; set; } = "irai";

    [Required]
    public string Password { get; set; } = "iraipass";

    [Required]
    public string Exchange { get; set; } = "request-exchange";
}
