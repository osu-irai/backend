using Npgsql;
using Testcontainers.PostgreSql;

namespace osuRequestor.Tests.Infrastructure.Fixtures;

public sealed class TestDatabaseFixture : IAsyncDisposable
{
    private PostgreSqlContainer? _container;

    private TestDatabaseFixture() { }

    public static TestDatabaseFixture Instance { get; } = new TestDatabaseFixture();

    public string ConnectionString { get; private set; } = "";

    public bool IsAvailable => this._container is not null;

    public string? UnavailableReason { get; private set; }

    #region IAsyncDisposable Members

    public async ValueTask DisposeAsync()
    {
        if (this._container is not null)
        {
            await this._container.DisposeAsync();
        }
    }

    #endregion

    public async Task StartAsync()
    {
        if (this._container is not null)
        {
            return;
        }

        try
        {
            this._container = new PostgreSqlBuilder("postgres:18")
                              .WithDatabase("irai_tests")
                              .WithUsername("irai")
                              .WithPassword("irai")
                              .Build();

            await this._container.StartAsync();
            this.ConnectionString = this._container.GetConnectionString();
        }
        catch (Exception exception)
        {
            this.UnavailableReason = exception.Message;
            this._container        = null;
        }
    }

    public async Task ResetAsync()
    {
        if (this._container is null)
        {
            return;
        }

        await using var connection = new NpgsqlConnection(this.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            TRUNCATE TABLE "Requests", "Beatmaps", "BeatmapSets", "Users", "Settings", "Twitch", "Tokens", "DataProtectionKeys"
            RESTART IDENTITY CASCADE;
            """,
            connection);

        await command.ExecuteNonQueryAsync();
    }
}
