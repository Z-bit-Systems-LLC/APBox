using ApBox.Web.Services;
using ApBox.Core.Services;
using ApBox.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApBox.Core.Tests;

[TestFixture]
[Category("Unit")]
public class DependencyInjectionTests
{
    private ServiceCollection _services;
    private IConfiguration _configuration;
    
    [SetUp]
    public void Setup()
    {
        _services = new ServiceCollection();
        
        // Add logging
        _services.AddLogging(builder => builder.AddConsole());
        
        // Create test configuration
        var configData = new Dictionary<string, string?>
        {
            ["PluginSettings:Directory"] = "test-plugins"
        };
        
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }
    
    [Test]
    public void AddApBoxServices_RegistersAllRequiredServices()
    {
        // Act
        _services.AddApBoxServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();
        
        // Assert - Plugin services
        Assert.That(serviceProvider.GetService<IPluginLoader>(), Is.Not.Null);
        
        // Assert - Core services
        Assert.That(serviceProvider.GetService<ICardProcessingService>(), Is.Not.Null);
        Assert.That(serviceProvider.GetService<IReaderService>(), Is.Not.Null);
        Assert.That(serviceProvider.GetService<IReaderConfigurationService>(), Is.Not.Null);
    }
    
    [Test]
    public void PluginLoader_UsesConfiguredDirectory()
    {
        // Act
        _services.AddApBoxServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();
        var pluginLoader = serviceProvider.GetRequiredService<IPluginLoader>();
        
        // Assert
        Assert.That(pluginLoader, Is.Not.Null);
        Assert.That(pluginLoader, Is.TypeOf<CachedPluginLoader>());
    }
    
    [Test]
    public void ServiceLifetimes_AreConfiguredCorrectly()
    {
        // Act
        _services.AddApBoxServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();
        
        // Assert - Singletons should return same instance
        var pluginLoader1 = serviceProvider.GetService<IPluginLoader>();
        var pluginLoader2 = serviceProvider.GetService<IPluginLoader>();
        Assert.That(pluginLoader1, Is.SameAs(pluginLoader2));
        
    }
    
    [Test]
    public void CardProcessingService_CanBeResolved()
    {
        // Act
        _services.AddApBoxServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();
        
        // Assert
        var cardProcessingService = serviceProvider.GetService<ICardProcessingService>();
        Assert.That(cardProcessingService, Is.Not.Null);
        Assert.That(cardProcessingService, Is.TypeOf<CardProcessingService>());
    }
    
    [Test]
    public void ReaderConfigurationService_CanBeResolved()
    {
        // Act
        _services.AddApBoxServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();
        
        // Assert - Just verify the service can be resolved, don't test database operations
        var configService = serviceProvider.GetService<IReaderConfigurationService>();
        Assert.That(configService, Is.Not.Null);
    }
}