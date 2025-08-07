using ApBox.Core.OSDP;
using ApBox.Core.Services;
using ApBox.Core.Services.Core;
using ApBox.Core.Services.Reader;
using ApBox.Core.Services.Configuration;
using ApBox.Core.Services.Persistence;
using ApBox.Core.Services.Infrastructure;
using ApBox.Core.Services.Plugins;
using ApBox.Core.Data;
using ApBox.Plugins;
using ApBox.Web.ViewModels;
using ApBox.Web.Services.Notifications;

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
            var loggerFactory = provider.GetService<ILoggerFactory>();
            return new CachedPluginLoader(pluginDirectory, logger, loggerFactory);
        });
        
        // Register OSDP services
        services.AddSingleton<IOsdpCommunicationManager, OsdpCommunicationManager>();
        services.AddHostedService<OsdpStartupService>();
        services.AddHostedService<OsdpStatusBridgeService>();
        services.AddHostedService<CardProcessingBridgeService>();
        services.AddHostedService<PinProcessingBridgeService>();
        
        // Register core application services
        services.AddSingleton<ICardProcessingService, CardProcessingService>();
        services.AddSingleton<ICardEventPersistenceService, CardEventPersistenceService>();
        services.AddSingleton<IReaderService, ReaderService>();
        
        // Register core event processing orchestrators
        services.AddSingleton<CardEventProcessingOrchestrator>();
        services.AddSingleton<PinEventProcessingOrchestrator>();
        
        // Register web orchestrators that handle notifications
        services.AddSingleton<ICardProcessingOrchestrator, CardProcessingWebOrchestrator>();
        services.AddSingleton<IPinProcessingOrchestrator, PinProcessingWebOrchestrator>();
        
        // Register enhanced services for SignalR integration
        services.AddSingleton<IEnhancedCardProcessingService, SignalRCardProcessingService>();
        
        // Register PIN processing services
        services.AddSingleton<IPinProcessingService, PinProcessingService>();
        services.AddSingleton<IPinEventPersistenceService, PinEventPersistenceService>();
        
        // Register unified notification aggregator
        services.AddSingleton<INotificationAggregator, SignalRNotificationAggregator>();
        
        
        // Register ViewModels
        services.AddScoped<DashboardViewModel>();
        services.AddScoped<CardEventsViewModel>();
        services.AddScoped<ReadersConfigurationViewModel>();
        services.AddScoped<FeedbackConfigurationViewModel>();
        services.AddScoped<PluginsConfigurationViewModel>();
        services.AddScoped<SystemConfigurationViewModel>();
        
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