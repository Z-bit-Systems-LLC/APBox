using ApBox.Plugins;
using ApBox.Core.OSDP;
using ApBox.Core.Services;
using ApBox.Core.Data;
using ApBox.Web.ViewModels;

namespace ApBox.Web.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApBoxServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register caching service
        services.AddSingleton<ICacheService, MemoryCacheService>();
        
        // Register plugin system services with enhanced caching
        services.AddSingleton<IPluginLoader>(provider =>
        {
            var pluginDirectory = configuration.GetValue<string>("PluginSettings:Directory") ?? "plugins";
            var logger = provider.GetService<ILogger<CachedPluginLoader>>();
            return new CachedPluginLoader(pluginDirectory, logger);
        });
        
        // Register OSDP services
        services.AddSingleton<IOsdpCommunicationManager, OsdpCommunicationManager>();
        services.AddHostedService<OsdpStartupService>();
        services.AddHostedService<OsdpStatusBridgeService>();
        services.AddHostedService<CardProcessingBridgeService>();
        
        // Register core application services
        services.AddSingleton<ICardProcessingService, CardProcessingService>();
        services.AddSingleton<ICardEventPersistenceService, CardEventPersistenceService>();
        services.AddSingleton<ICardProcessingOrchestrator, CardProcessingOrchestrator>();
        services.AddSingleton<IEnhancedCardProcessingService, EnhancedCardProcessingService>();
        services.AddSingleton<IReaderService, ReaderService>();
        
        // Register SignalR notification service
        services.AddSingleton<ICardEventNotificationService, CardEventNotificationService>();
        
        // Register SignalR factory and wrapper
        services.AddTransient<IHubConnectionFactory, HubConnectionFactory>();
        services.AddTransient<IHubConnectionWrapper>(sp => 
            sp.GetRequiredService<IHubConnectionFactory>().CreateConnection());
        
        // Register ViewModels
        services.AddScoped<DashboardViewModel>();
        services.AddScoped<ReadersConfigurationViewModel>();
        services.AddScoped<FeedbackConfigurationViewModel>();
        
        // Register system management services
        services.AddScoped<IConfigurationExportService, ConfigurationExportService>();
        services.AddScoped<ISystemRestartService, SystemRestartService>();
        services.AddSingleton<ILogService, LogService>();
        
        // Register database services
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=apbox.db";
        services.AddApBoxDatabase(connectionString);
        
        return services;
    }
}