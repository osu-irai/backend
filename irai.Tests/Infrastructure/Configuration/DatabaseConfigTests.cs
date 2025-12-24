using System.ComponentModel.DataAnnotations;
using NUnit.Framework;
using osuRequestor.Infrastructure.Configuration;

namespace osuRequestor.Tests.Infrastructure.Configuration;

[TestFixture]
public class DatabaseConfigTests
{
    [Test]
    public void ToConnection_MapsConfiguredValues()
    {
        var config = new DatabaseConfig
        {
            Host     = "db.internal",
            Port     = 5433,
            Database = "requests",
            Username = "irai",
            Password = "secret"
        };

        var connection = config.ToConnection();

        Assert.That(connection.Host,     Is.EqualTo("db.internal"));
        Assert.That(connection.Port,     Is.EqualTo(5433));
        Assert.That(connection.Database, Is.EqualTo("requests"));
        Assert.That(connection.Username, Is.EqualTo("irai"));
        Assert.That(connection.Password, Is.EqualTo("secret"));
    }

    [Test]
    public void Validation_RejectsPortOutsideTcpRange()
    {
        var config = new DatabaseConfig
        {
            Host     = "localhost",
            Port     = 0,
            Database = "requests",
            Username = "irai",
            Password = "secret"
        };

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(
            config,
            new ValidationContext(config),
            results,
            true);

        Assert.That(isValid,                                          Is.False);
        Assert.That(results.SelectMany(result => result.MemberNames), Does.Contain(nameof(DatabaseConfig.Port)));
    }
}
