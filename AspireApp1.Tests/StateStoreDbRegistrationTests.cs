using AspireApp1.StateStore;
using Microsoft.Extensions.Configuration;

namespace AspireApp1.Tests;

[TestClass]
public class StateStoreDbRegistrationTests
{
    [TestMethod]
    public void ResolveProvider_DefaultsToSqlite()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var provider = StateStoreDbRegistration.ResolveProvider(configuration);

        Assert.AreEqual("sqlite", provider);
    }

    [TestMethod]
    public void ResolveConnectionString_UsesSqlServerConnectionWhenConfigured()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StateStore:Provider"] = "SqlServer",
                ["ConnectionStrings:statestoreSqlServer"] = "Server=.;Database=AspireApp1StateStore;Trusted_Connection=True;TrustServerCertificate=True;"
            })
            .Build();

        var provider = StateStoreDbRegistration.ResolveProvider(configuration);
        var connectionString = StateStoreDbRegistration.ResolveConnectionString(configuration, provider);

        StringAssert.Contains(connectionString, "Server=.");
        StringAssert.Contains(connectionString, "Database=AspireApp1StateStore");
    }
}
