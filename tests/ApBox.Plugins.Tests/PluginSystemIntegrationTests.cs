using ApBox.Plugins;

namespace ApBox.Plugins.Tests;

[TestFixture]
[Category("Integration")]
public class PluginSystemIntegrationTests
{
    private PluginLoader _pluginLoader;
    private FeedbackResolutionService _feedbackService;
    private string _testPluginDirectory;
    
    [SetUp]
    public void Setup()
    {
        _testPluginDirectory = Path.Combine(Path.GetTempPath(), "ApBoxTestPlugins", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testPluginDirectory);
        _pluginLoader = new PluginLoader(_testPluginDirectory);
        _feedbackService = new FeedbackResolutionService();
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
    public async Task EndToEndWorkflow_WithMockPlugin_ProcessesCardAndReturnsFeedback()
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
        var cardResult = new CardReadResult { Success = processResult, Message = "Test processed" };
        var feedback = await _feedbackService.ResolveFeedbackAsync(readerId, cardResult, mockPlugin);
        
        // Assert
        Assert.That(processResult, Is.True);
        Assert.That(feedback, Is.Not.Null);
        Assert.That(feedback.Type, Is.EqualTo(ReaderFeedbackType.Custom));
        Assert.That(feedback.BeepCount, Is.EqualTo(2));
        Assert.That(feedback.LedColor, Is.EqualTo(LedColor.Blue));
    }
    
    [Test]
    public async Task PluginLifecycle_InitializeAndShutdown_ExecutesCorrectly()
    {
        // Arrange
        var plugin = new TestPlugin();
        
        // Act & Assert - Initialize
        Assert.DoesNotThrowAsync(async () => await plugin.InitializeAsync());
        Assert.That(((TestPlugin)plugin).IsInitialized, Is.True);
        
        // Act & Assert - Shutdown
        Assert.DoesNotThrowAsync(async () => await plugin.ShutdownAsync());
        Assert.That(((TestPlugin)plugin).IsShutdown, Is.True);
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
    
    [Test]
    public async Task FeedbackResolution_WithMultipleSources_ReturnsHighestPriority()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var result = new CardReadResult { Success = true };
        var plugin = new TestPlugin();
        
        // Act
        var feedback = await _feedbackService.ResolveFeedbackAsync(readerId, result, plugin);
        
        // Assert
        Assert.That(feedback, Is.Not.Null);
        Assert.That(feedback.Type, Is.EqualTo(ReaderFeedbackType.Custom)); // Plugin has higher priority
    }
    
    private class TestPlugin : IApBoxPlugin
    {
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
        
        public Task<ReaderFeedback?> GetFeedbackAsync(CardReadResult result)
        {
            return Task.FromResult<ReaderFeedback?>(new ReaderFeedback
            {
                Type = ReaderFeedbackType.Custom,
                BeepCount = 2,
                LedColor = LedColor.Blue,
                LedDurationMs = 1500
            });
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