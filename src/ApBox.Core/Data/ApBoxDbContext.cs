using System.Data;
using Microsoft.Data.Sqlite;
using ApBox.Core.Data.Migrations;
using ApBox.Core.Data.Mappers;

namespace ApBox.Core.Data;

public class ApBoxDbContext(
    string connectionString,
    ILogger<ApBoxDbContext> logger,
    IMigrationRunner? migrationRunner = null)
    : IApBoxDbContext
{
    static ApBoxDbContext()
    {
        // Configure Dapper to use snake_case to PascalCase mapping
        DapperMappingExtensions.ConfigureSnakeCaseMapping();
    }

    // Constructor for migration runner to avoid circular dependency

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