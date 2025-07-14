using System.Data;
using System.Reflection;
using Dapper;
using Microsoft.Extensions.Logging;

namespace ApBox.Core.Data.Migrations;

public interface IMigrationRunner
{
    Task RunMigrationsAsync();
    
    Task<IEnumerable<string>> GetAppliedMigrationsAsync();
    Task<IEnumerable<string>> GetPendingMigrationsAsync();
}

public class MigrationRunner(IApBoxDbContext dbContext, ILogger<MigrationRunner> logger) : IMigrationRunner
{
    public async Task RunMigrationsAsync()
    {
        await EnsureMigrationsTableExistsAsync();
        
        var pendingMigrations = await GetPendingMigrationsAsync();
        
        foreach (var migrationVersion in pendingMigrations)
        {
            await ApplyMigrationAsync(migrationVersion);
        }
    }
    
    public async Task<IEnumerable<string>> GetAppliedMigrationsAsync()
    {
        using var connection = dbContext.CreateDbConnectionAsync();
        connection.Open();
        
        try
        {
            var sql = "SELECT version FROM schema_migrations ORDER BY version";
            var versions = await connection.QueryAsync<string>(sql);
            return versions;
        }
        catch (Exception ex) when (ex.Message.Contains("no such table"))
        {
            // The migration table doesn't exist yet
            return [];
        }
    }
    
    public async Task<IEnumerable<string>> GetPendingMigrationsAsync()
    {
        var appliedMigrations = await GetAppliedMigrationsAsync();
        var availableMigrations = GetAvailableMigrations();
        
        return availableMigrations.Except(appliedMigrations).OrderBy(v => v);
    }
    
    private async Task EnsureMigrationsTableExistsAsync()
    {
        using var connection = dbContext.CreateDbConnectionAsync();
        connection.Open();
        
        var sql = @"
            CREATE TABLE IF NOT EXISTS schema_migrations (
                version TEXT PRIMARY KEY,
                applied_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                description TEXT
            )";
            
        await connection.ExecuteAsync(sql);
    }
    
    private async Task ApplyMigrationAsync(string version)
    {
        var migrationFile = GetMigrationFilePath(version);
        if (!File.Exists(migrationFile))
        {
            throw new FileNotFoundException($"Migration file not found: {migrationFile}");
        }
        
        var migrationSql = await File.ReadAllTextAsync(migrationFile);
        var description = ExtractDescriptionFromSql(migrationSql);

        using var connection = dbContext.CreateDbConnectionAsync();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        
        try
        {
            // Execute migration SQL
            await connection.ExecuteAsync(migrationSql, transaction: transaction);
            
            // Record migration as applied
            var recordSql = @"
                INSERT INTO schema_migrations (version, description, applied_at) 
                VALUES (@Version, @Description, @AppliedAt)";
                
            await connection.ExecuteAsync(recordSql, new
            {
                Version = version,
                Description = description,
                AppliedAt = DateTime.UtcNow
            }, transaction: transaction);
            
            transaction.Commit();
            
            logger.LogInformation("Applied migration {Version}: {Description}", version, description);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            logger.LogError(ex, "Failed to apply migration {Version}", version);
            throw;
        }
    }
    
    private IEnumerable<string> GetAvailableMigrations()
    {
        var migrationsDirectory = GetMigrationsDirectory();
        if (!Directory.Exists(migrationsDirectory))
        {
            return [];
        }
        
        return Directory.GetFiles(migrationsDirectory, "*.sql")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(f => f != null && f.Contains('_'))
            .Select(f => f?.Split('_')[0] ?? string.Empty)
            .OrderBy(v => v);
    }
    
    private string GetMigrationsDirectory()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        
        // Try the output directory structure first (Data/Migrations)
        var migrationsPath = Path.Combine(assemblyDirectory!, "Data", "Migrations");
        return Directory.Exists(migrationsPath) ? migrationsPath :
            // Fallback to a simple Migrations directory
            Path.Combine(assemblyDirectory!, "Migrations");
    }
    
    private string GetMigrationFilePath(string version)
    {
        var migrationsDirectory = GetMigrationsDirectory();
        var migrationFiles = Directory.GetFiles(migrationsDirectory, $"{version}_*.sql");

        return migrationFiles.Length switch
        {
            0 => throw new FileNotFoundException($"No migration file found for version {version}"),
            > 1 => throw new InvalidOperationException($"Multiple migration files found for version {version}"),
            _ => migrationFiles[0]
        };
    }
    
    private string ExtractDescriptionFromSql(string sql)
    {
        var lines = sql.Split('\n');
        var descriptionLine = lines.FirstOrDefault(l => l.TrimStart().StartsWith("-- Description:"));
        
        if (descriptionLine != null)
        {
            return descriptionLine.Replace("-- Description:", "").Trim();
        }
        
        return "No description provided";
    }
}