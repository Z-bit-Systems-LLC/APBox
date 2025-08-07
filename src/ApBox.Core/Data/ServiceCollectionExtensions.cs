using ApBox.Core.Data.Repositories;
using ApBox.Core.Data.Migrations;
using ApBox.Core.Services.Reader;
using ApBox.Core.Services.Configuration;
using ApBox.Core.Services.Core;
using ApBox.Core.Services.Security;
using ApBox.Core.Services.Persistence;
using ApBox.Core.Services.Infrastructure;
using ApBox.Core.Services.Events;

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
            var fileSystem = provider.GetRequiredService<IFileSystem>();
            // Create a simple context for migration runner to avoid circular dependency
            var simpleContext = new ApBoxDbContext(connectionString, dbLogger);
            return new MigrationRunner(simpleContext, fileSystem, migrationLogger);
        });
        
        // Register database context
        services.AddSingleton<IApBoxDbContext>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<ApBoxDbContext>>();
            var migrationRunner = provider.GetRequiredService<IMigrationRunner>();
            return new ApBoxDbContext(connectionString, logger, migrationRunner);
        });
        
        // Register repositories
        services.AddSingleton<IReaderConfigurationRepository, ReaderConfigurationRepository>();
        services.AddSingleton<ICardEventRepository, CardEventRepository>();
        services.AddSingleton<IPluginConfigurationRepository, PluginConfigurationRepository>();
        services.AddSingleton<IFeedbackConfigurationRepository, FeedbackConfigurationRepository>();
        services.AddSingleton<IReaderPluginMappingRepository, ReaderPluginMappingRepository>();
        services.AddSingleton<IPinEventRepository, PinEventRepository>();
        
        // Register database-backed services
        services.AddSingleton<IReaderConfigurationService>(provider =>
        {
            var repository = provider.GetRequiredService<IReaderConfigurationRepository>();
            var logger = provider.GetRequiredService<ILogger<ReaderConfigurationService>>();
            var lazyReaderService = new Lazy<IReaderService>(() => provider.GetRequiredService<IReaderService>());
            return new ReaderConfigurationService(repository, logger, lazyReaderService);
        });
        services.AddSingleton<IFeedbackConfigurationService, FeedbackConfigurationService>();
        services.AddSingleton<IReaderPluginMappingService, ReaderPluginMappingService>();
        
        // Register OSDP services
        services.AddSingleton<IOsdpSecurityService, OsdpSecurityService>();
        services.AddSingleton<ISecurityModeUpdateService, SecurityModeUpdateService>();
        services.AddSingleton<ISerialPortService, SerialPortService>();
        services.AddSingleton<IReaderService, ReaderService>();
        
        // Register PIN collection service
        services.AddSingleton<IPinCollectionService, PinCollectionService>();
        
        // Register file system abstraction
        services.AddSingleton<IFileSystem, FileSystem>();
        
        // Register encryption services
        services.AddSingleton<IEncryptionKeyService, EncryptionKeyService>();
        services.AddSingleton<IDataEncryptionService, DataEncryptionService>();
        
        // Register PIN event persistence service
        services.AddSingleton<IPinEventPersistenceService, PinEventPersistenceService>();
        
        // Register event publishing services
        services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();
        
        // Register processing services
        services.AddSingleton<ICardProcessingService, CardProcessingService>();
        services.AddSingleton<IPinProcessingService, PinProcessingService>();
        services.AddSingleton<ICardEventPersistenceService, CardEventPersistenceService>();
        
        // Register orchestrators
        services.AddSingleton<CardEventProcessingOrchestrator>();
        services.AddSingleton<PinEventProcessingOrchestrator>();
        
        // Register pipeline and bridge service (pipeline needs repository for enrichment)
        services.AddSingleton<IEventProcessingPipeline>(provider =>
        {
            var cardOrchestrator = provider.GetRequiredService<CardEventProcessingOrchestrator>();
            var pinOrchestrator = provider.GetRequiredService<PinEventProcessingOrchestrator>();
            var eventPublisher = provider.GetRequiredService<IEventPublisher>();
            var readerConfigRepository = provider.GetRequiredService<IReaderConfigurationRepository>();
            var logger = provider.GetRequiredService<ILogger<EventProcessingPipeline>>();
            return new EventProcessingPipeline(cardOrchestrator, pinOrchestrator, eventPublisher, readerConfigRepository, logger);
        });
        services.AddHostedService<EventBridgeService>();
        
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
        var logger = serviceProvider.GetRequiredService<ILogger<ReaderConfigurationService>>();
        logger.LogInformation("Starting with empty reader configuration database - no seed data created");
        await Task.CompletedTask;
    }
}