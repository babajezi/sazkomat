using Microsoft.EntityFrameworkCore;
using Sazkomat.Configuration.Data;
using Sazkomat.Data.Data;

namespace Sazkomat.Tests.Fixtures;

/// <summary>
/// Shared database fixture for test collections.
/// Provides reusable DbContext instances for multiple tests.
/// </summary>
public class DatabaseFixture : IDisposable
{
    private bool _disposed = false;

    /// <summary>
    /// Creates a new ConfigurationDbContext with in-memory database
    /// </summary>
    public ConfigurationDbContext CreateConfigurationDbContext()
    {
        var options = new DbContextOptionsBuilder<ConfigurationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ConfigurationDbContext(options);
    }

    /// <summary>
    /// Creates a new DataDbContext with in-memory database
    /// </summary>
    public DataDbContext CreateDataDbContext()
    {
        var options = new DbContextOptionsBuilder<DataDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new DataDbContext(options);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Cleanup if needed
            }
            _disposed = true;
        }
    }
}

/// <summary>
/// Collection definition for tests that need database access.
/// Usage: [Collection("Database")]
/// </summary>
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is to be the place
    // to apply [CollectionDefinition] and all the ICollectionFixture<> interfaces.
}
