using System.Data;
using Microsoft.Data.Sqlite;
using ApBox.Core.Data.Migrations;

namespace ApBox.Core.Data;

public class ApBoxDbContext(string connectionString, ILogger<ApBoxDbContext> logger, IMigrationRunner migrationRunner)
    : IApBoxDbContext
{
    // Constructor for migration runner to avoid circular dependency
    public ApBoxDbContext(string connectionString, ILogger<ApBoxDbContext> logger) : this(connectionString, logger, null)
    {
    }

    public IDbConnection CreateDbConnectionAsync()
    {
        return new SqliteConnection(connectionString);
    }

    public async Task InitializeDatabaseAsync()
    {
        logger.LogInformation("Initializing ApBox database...");

        // Run migrations to create/update schema
        if (migrationRunner != null)
        {
            await migrationRunner.RunMigrationsAsync();
        }
        else
        {
            logger.LogWarning("Migration runner is not available - skipping migrations");
        }
        
        logger.LogInformation("Database initialization completed");
    }
}