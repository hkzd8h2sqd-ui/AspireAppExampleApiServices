using AspireApp1.StateStore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AspireApp1.Tests;

/// <summary>
/// Creates a real, file-backed SQLite state store for tests.
/// </summary>
/// <remarks>
/// The EF Core in-memory provider accepts queries that SQLite cannot translate
/// (e.g. <see cref="DateTimeOffset"/> in <c>ORDER BY</c>), so queries used by the UI
/// should also be exercised against SQLite, which is the default provider.
/// </remarks>
internal static class SqliteTestDatabase
{
    /// <summary>
    /// Creates a context factory for a fresh SQLite database with the schema initialized.
    /// </summary>
    /// <returns>A factory for <see cref="StateStoreDbContext"/> backed by a temporary SQLite file.</returns>
    public static async Task<IDbContextFactory<StateStoreDbContext>> CreateFactoryAsync()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), "AspireApp1Tests", $"{Guid.NewGuid():N}.db");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StateStore:Provider"] = "Sqlite",
                ["ConnectionStrings:statestore"] = $"Data Source={dbPath}"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddConfiguredStateStoreDbContextFactory(configuration);
        var factory = services.BuildServiceProvider().GetRequiredService<IDbContextFactory<StateStoreDbContext>>();

        await using var db = await factory.CreateDbContextAsync();
        await DatabaseInitializer.EnsureSchemaAsync(db);
        return factory;
    }
}
