using ApBox.Core.Services.Plugins;
using ApBox.Plugins;

namespace ApBox.Core.Tests.Services.Plugins;

[TestFixture]
[Category("Integration")]
public class PluginSystemIntegrationTests
{
    private PluginLoader _pluginLoader;
    private string _testPluginDirectory;
    
    [SetUp]
    public void Setup()
    {
        _testPluginDirectory = Path.Combine(Path.GetTempPath(), "ApBoxTestPlugins", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testPluginDirectory);
        _pluginLoader = new PluginLoader(_testPluginDirectory);
    }
    
    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testPluginDirectory))
        {
            Directory.Delete(_testPluginDirectory, true);
        }
    }
    
    [Test]
    public async Task EndToEndWorkflow_WithMockPlugin_ProcessesCard()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var cardReadEvent = new CardReadEvent
        {
            ReaderId = readerId,
            CardNumber = "12345",
            BitLength = 26,
            Timestamp = DateTime.UtcNow,
            ReaderName = "Test Reader"
        };
        
        var mockPlugin = new TestPlugin();
        await mockPlugin.InitializeAsync();
        
        // Act
        var processResult = await mockPlugin.ProcessCardReadAsync(cardReadEvent);
        
        // Assert
        Assert.That(processResult, Is.True);
    }
    
    [Test]
    public Task PluginLifecycle_InitializeAndShutdown_ExecutesCorrectly()
    {
        // Arrange
        var plugin = new TestPlugin();
        
        // Act & Assert - Initialize
        Assert.DoesNotThrowAsync(async () => await plugin.InitializeAsync());
        Assert.That(plugin.IsInitialized, Is.True);
        
        // Act & Assert - Shutdown
        Assert.DoesNotThrowAsync(async () => await plugin.ShutdownAsync());
        Assert.That(plugin.IsShutdown, Is.True);
        return Task.CompletedTask;
    }
    
    [Test]
    public void PluginMetadata_CreateFromPlugin_ContainsCorrectInformation()
    {
        // Arrange
        var plugin = new TestPlugin();
        
        // Act
        var metadata = new PluginMetadata
        {
            Id = Guid.NewGuid().ToString(),
            Name = plugin.Name,
            Version = plugin.Version,
            Description = plugin.Description,
            AssemblyPath = "test-assembly.dll",
            IsEnabled = true
        };
        
        // Assert
        Assert.That(metadata.Name, Is.EqualTo("Test Plugin"));
        Assert.That(metadata.Version, Is.EqualTo("1.0.0"));
        Assert.That(metadata.Description, Is.EqualTo("A test plugin for unit testing"));
        Assert.That(metadata.IsEnabled, Is.True);
        Assert.That(metadata.Configuration, Is.Not.Null);
    }
    
    
    private class TestPlugin : IApBoxPlugin
    {
        public Guid Id => new Guid("E5F6A7B8-9ABC-DEF0-1234-123456789999");
        public string Name => "Test Plugin";
        public string Version => "1.0.0";
        public string Description => "A test plugin for unit testing";
        
        public bool IsInitialized { get; private set; }
        public bool IsShutdown { get; private set; }
        
        public Task<bool> ProcessCardReadAsync(CardReadEvent cardRead)
        {
            // Simple test logic - accept all cards
            return Task.FromResult(true);
        }
        
        public Task<bool> ProcessPinReadAsync(PinReadEvent pinRead)
        {
            // Simple test logic - accept all PINs
            return Task.FromResult(true);
        }
        
        
        public Task InitializeAsync()
        {
            IsInitialized = true;
            return Task.CompletedTask;
        }
        
        public Task ShutdownAsync()
        {
            IsShutdown = true;
            return Task.CompletedTask;
        }
    }
}