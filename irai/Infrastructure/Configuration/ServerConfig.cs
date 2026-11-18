using System.ComponentModel.DataAnnotations;

namespace irai.Infrastructure.Configuration;

public class ServerConfig
{
    public const string Position = "ServerConfig";

    [Required]
    public string HomePage { get; init; } = null!;
}
