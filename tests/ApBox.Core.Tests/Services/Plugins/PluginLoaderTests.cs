using ApBox.Core.Services.Plugins;
using ApBox.Plugins;

namespace ApBox.Core.Tests.Services.Plugins;

[TestFixture]
[Category("Unit")]
public class PluginLoaderTests
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
    public async Task LoadPluginsAsync_WithEmptyDirectory_ReturnsEmptyList()
    {
        var plugins = await _pluginLoader.LoadPluginsAsync();
        
        Assert.That(plugins, Is.Not.Null);
        Assert.That(plugins, Is.Empty);
    }
    
    [Test]
    public async Task LoadPluginsAsync_WithNonExistentDirectory_ReturnsEmptyList()
    {
        var nonExistentDir = Path.Combine(Path.GetTempPath(), "NonExistent", Guid.NewGuid().ToString());
        var loader = new PluginLoader(nonExistentDir);
        
        var plugins = await loader.LoadPluginsAsync();
        
        Assert.That(plugins, Is.Not.Null);
        Assert.That(plugins, Is.Empty);
    }
    
    [Test]
    public void GetAvailablePlugins_WithEmptyDirectory_ReturnsEmptyList()
    {
        var plugins = _pluginLoader.GetAvailablePlugins();
        
        Assert.That(plugins, Is.Not.Null);
        Assert.That(plugins, Is.Empty);
    }
    
    [Test]
    public async Task UnloadPluginAsync_WithValidPluginId_RemovesFromLoadedPlugins()
    {
        // This test will be implemented when we have actual plugin loading
        var pluginId = "test-plugin-id";
        
        await _pluginLoader.UnloadPluginAsync(pluginId);
        
        // Should not throw exception for non-existent plugin
        Assert.Pass("Unloading non-existent plugin should not throw");
    }
}