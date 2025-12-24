using System.ComponentModel.DataAnnotations;

namespace osuRequestor.Infrastructure.Configuration;

public class AuthClient
{
    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string ClientSecret { get; set; } = null!;
}
