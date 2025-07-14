using ApBox.Core.Data.Repositories;
using ApBox.Core.Data.Migrations;
using ApBox.Core.Services;
using ApBox.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApBox.Core.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApBoxDatabase(this IServiceCollection services, string connectionString)
    {
        // Register migration runner first (no dependencies)
        services.AddSingleton<IMigrationRunner>(provider =>
        {
            var migrationLogger = provider.GetRequiredService<ILogger<MigrationRunner>>();
            var dbLogger = provider.GetRequiredService<ILogger<ApBoxDbContext>>();
            // Create a simple context for migration runner to avoid circular dependency
            var simpleContext = new ApBoxDbContext(connectionString, dbLogger);
            return new MigrationRunner(simpleContext, migrationLogger);
        });
        
        // Register database context
        services.AddSingleton<IApBoxDbContext>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<ApBoxDbContext>>();
            var migrationRunner = provider.GetRequiredService<IMigrationRunner>();
            return new ApBoxDbContext(connectionString, logger, migrationRunner);
        });
        
        // Register repositories
        services.AddScoped<IReaderConfigurationRepository, ReaderConfigurationRepository>();
        services.AddScoped<ICardEventRepository, CardEventRepository>();
        services.AddScoped<IPluginConfigurationRepository, PluginConfigurationRepository>();
        
        // Register database-backed services
        services.AddScoped<IReaderConfigurationService, ReaderConfigurationService>();
        
        return services;
    }
    
    public static async Task InitializeApBoxDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApBoxDbContext>();
        await dbContext.InitializeDatabaseAsync();
        
        // Optionally seed with default data
        await SeedDefaultDataAsync(scope.ServiceProvider);
    }
    
    private static async Task SeedDefaultDataAsync(IServiceProvider serviceProvider)
    {
        var readerRepo = serviceProvider.GetRequiredService<IReaderConfigurationRepository>();
        var logger = serviceProvider.GetRequiredService<ILogger<ReaderConfigurationService>>();
        
        // Check if we already have readers
        var existingReaders = await readerRepo.GetAllAsync();
        if (existingReaders.Any())
        {
            logger.LogInformation("Database already contains {Count} reader configurations", existingReaders.Count());
            return;
        }
        
        // Seed with default readers
        var defaultReaders = new[]
        {
            new ReaderConfiguration
            {
                ReaderId = Guid.Parse("12345678-1234-1234-1234-123456789abc"),
                ReaderName = "Main Entrance"
            },
            new ReaderConfiguration
            {
                ReaderId = Guid.Parse("87654321-4321-4321-4321-cba987654321"),
                ReaderName = "Loading Dock"
            }
        };
        
        foreach (var reader in defaultReaders)
        {
            await readerRepo.CreateAsync(reader);
        }
        
        logger.LogInformation("Seeded database with {Count} default reader configurations", defaultReaders.Length);
    }
}