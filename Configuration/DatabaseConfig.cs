using Npgsql;

namespace osuRequestor.Configuration;

public class DatabaseConfig
{
    public const string Position = "Database";
    public string Database { get; set; } = null!;
    public string Host { get; set; } = null!;
    public int Port { get; set; }
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;

    public NpgsqlConnectionStringBuilder ToConnection()
    {
        return new NpgsqlConnectionStringBuilder
        {
            Host = Host,
            Port = Port,
            Database = Database,
            Username = Username,
            Password = Password,
        };
    }
}