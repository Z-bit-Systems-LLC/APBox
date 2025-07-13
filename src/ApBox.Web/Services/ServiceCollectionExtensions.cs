using ApBox.Plugins;
using ApBox.Core.OSDP;
using ApBox.Core.Services;
using ApBox.Core.Data;

namespace ApBox.Web.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApBoxServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register plugin system services
        services.AddSingleton<IPluginLoader>(provider =>
        {
            var pluginDirectory = configuration.GetValue<string>("PluginSettings:Directory") ?? "plugins";
            return new PluginLoader(pluginDirectory);
        });
        
        services.AddSingleton<IFeedbackResolutionService, FeedbackResolutionService>();
        
        // Register OSDP services
        services.AddSingleton<IOsdpCommunicationManager, OsdpCommunicationManager>();
        
        // Register core application services
        services.AddSingleton<ICardProcessingService, CardProcessingService>();
        services.AddScoped<IEnhancedCardProcessingService, EnhancedCardProcessingService>();
        services.AddScoped<IReaderService, ReaderService>();
        
        // Register SignalR notification service
        services.AddSingleton<ICardEventNotificationService, CardEventNotificationService>();
        
        // Register database services
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=apbox.db";
        services.AddApBoxDatabase(connectionString);
        
        return services;
    }
}