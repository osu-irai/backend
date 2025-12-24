using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using osuRequestor.Infrastructure.Persistence;
using osuRequestor.Tests.Infrastructure.Fixtures;

namespace osuRequestor.Tests.Infrastructure.Harness;

[NonParallelizable]
public abstract class IntegrationTestBase
{
    protected CustomWebApplicationFactory Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await TestDatabaseFixture.Instance.StartAsync();

        Assume.That(
            TestDatabaseFixture.Instance.IsAvailable,
            TestDatabaseFixture.Instance.UnavailableReason ?? "PostgreSQL test container is unavailable.");

        this.Factory = new CustomWebApplicationFactory(TestDatabaseFixture.Instance);
        this.Client  = this.Factory.CreateClient();
    }

    [SetUp]
    public Task ResetDatabase()
    {
        return TestDatabaseFixture.Instance.ResetAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        this.Client?.Dispose();

        if (this.Factory is not null)
        {
            await this.Factory.DisposeAsync();
        }
    }

    protected async Task SeedAsync(Func<DatabaseContext, Task> seed)
    {
        using var scope     = this.Factory.Services.CreateScope();
        var       dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        await seed(dbContext);
        await dbContext.SaveChangesAsync();
    }
}
