using ApBox.Plugins;
using ApBox.Core.OSDP;

namespace ApBox.Core.Services;

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
        services.AddSingleton<IReaderService, ReaderService>();
        
        // Register configuration services
        services.AddSingleton<IReaderConfigurationService, ReaderConfigurationService>();
        
        return services;
    }
}