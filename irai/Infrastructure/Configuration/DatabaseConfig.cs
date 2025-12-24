using System.ComponentModel.DataAnnotations;
using Npgsql;

namespace osuRequestor.Infrastructure.Configuration;

public class DatabaseConfig
{
    public const string Position = "Database";

    [Required]
    public string Database { get; set; } = null!;

    [Required]
    public string Host { get; set; } = null!;

    [Range(1, 65535)]
    public int Port { get; set; }

    [Required]
    public string Username { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;

    public NpgsqlConnectionStringBuilder ToConnection()
    {
        return new NpgsqlConnectionStringBuilder
        {
            Host     = this.Host,
            Port     = this.Port,
            Database = this.Database,
            Username = this.Username,
            Password = this.Password
        };
    }
}
