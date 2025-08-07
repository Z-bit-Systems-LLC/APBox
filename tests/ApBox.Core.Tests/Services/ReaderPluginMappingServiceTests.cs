using Microsoft.Extensions.Logging;
using Moq;
using ApBox.Core.Data.Models;
using ApBox.Core.Data.Repositories;
using ApBox.Core.Services.Configuration;

namespace ApBox.Core.Tests.Services;

[TestFixture]
public class ReaderPluginMappingServiceTests
{
    private Mock<IReaderPluginMappingRepository> _mockRepository;
    private Mock<ILogger<ReaderPluginMappingService>> _mockLogger;
    private IReaderPluginMappingService _service;

    [SetUp]
    public void Setup()
    {
        _mockRepository = new Mock<IReaderPluginMappingRepository>();
        _mockLogger = new Mock<ILogger<ReaderPluginMappingService>>();
        _service = new ReaderPluginMappingService(_mockRepository.Object, _mockLogger.Object);
    }

    [Test]
    public async Task GetPluginsForReaderAsync_ReturnsEnabledPluginsInOrder()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var mappings = new List<ReaderPluginMappingEntity>
        {
            new() { ReaderId = readerId.ToString(), PluginId = "Plugin3", ExecutionOrder = 3, IsEnabled = true },
            new() { ReaderId = readerId.ToString(), PluginId = "Plugin1", ExecutionOrder = 1, IsEnabled = true },
            new() { ReaderId = readerId.ToString(), PluginId = "Plugin2", ExecutionOrder = 2, IsEnabled = false },
            new() { ReaderId = readerId.ToString(), PluginId = "Plugin4", ExecutionOrder = 4, IsEnabled = true }
        };

        _mockRepository.Setup(r => r.GetMappingsForReaderAsync(readerId))
            .ReturnsAsync(mappings);

        // Act
        var result = (await _service.GetPluginsForReaderAsync(readerId)).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(3)); // Only enabled plugins
        Assert.That(result[0], Is.EqualTo("Plugin1"));
        Assert.That(result[1], Is.EqualTo("Plugin3"));
        Assert.That(result[2], Is.EqualTo("Plugin4"));
    }

    [Test]
    public async Task SetPluginsForReaderAsync_DeletesOldAndCreatesNewMappings()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var pluginIds = new[] { "Plugin1", "Plugin2", "Plugin3" };

        // Act
        await _service.SetPluginsForReaderAsync(readerId, pluginIds);

        // Assert
        _mockRepository.Verify(r => r.DeleteMappingsForReaderAsync(readerId), Times.Once);
        
        _mockRepository.Verify(r => r.CreateMappingAsync(It.Is<ReaderPluginMappingEntity>(m =>
            m.ReaderId == readerId.ToString() &&
            m.PluginId == "Plugin1" &&
            m.ExecutionOrder == 1 &&
            m.IsEnabled == true)), Times.Once);
            
        _mockRepository.Verify(r => r.CreateMappingAsync(It.Is<ReaderPluginMappingEntity>(m =>
            m.ReaderId == readerId.ToString() &&
            m.PluginId == "Plugin2" &&
            m.ExecutionOrder == 2 &&
            m.IsEnabled == true)), Times.Once);
            
        _mockRepository.Verify(r => r.CreateMappingAsync(It.Is<ReaderPluginMappingEntity>(m =>
            m.ReaderId == readerId.ToString() &&
            m.PluginId == "Plugin3" &&
            m.ExecutionOrder == 3 &&
            m.IsEnabled == true)), Times.Once);
    }

    [Test]
    public async Task UpdatePluginOrderAsync_CallsRepositoryMethod()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var pluginId = "TestPlugin";
        var newOrder = 5;

        // Act
        await _service.UpdatePluginOrderAsync(readerId, pluginId, newOrder);

        // Assert
        _mockRepository.Verify(r => r.UpdateExecutionOrderAsync(readerId, pluginId, newOrder), Times.Once);
    }

    [Test]
    public async Task EnablePluginForReaderAsync_CallsRepositoryWithTrue()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var pluginId = "TestPlugin";

        // Act
        await _service.EnablePluginForReaderAsync(readerId, pluginId);

        // Assert
        _mockRepository.Verify(r => r.SetPluginEnabledAsync(readerId, pluginId, true), Times.Once);
    }

    [Test]
    public async Task DisablePluginForReaderAsync_CallsRepositoryWithFalse()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var pluginId = "TestPlugin";

        // Act
        await _service.DisablePluginForReaderAsync(readerId, pluginId);

        // Assert
        _mockRepository.Verify(r => r.SetPluginEnabledAsync(readerId, pluginId, false), Times.Once);
    }

    [Test]
    public async Task GetAllMappingsAsync_ReturnsTransformedMappings()
    {
        // Arrange
        var mappings = new List<ReaderPluginMappingEntity>
        {
            new() { ReaderId = Guid.NewGuid().ToString(), PluginId = "Plugin1", ExecutionOrder = 1, IsEnabled = true },
            new() { ReaderId = Guid.NewGuid().ToString(), PluginId = "Plugin2", ExecutionOrder = 2, IsEnabled = false }
        };

        _mockRepository.Setup(r => r.GetAllMappingsAsync())
            .ReturnsAsync(mappings);

        // Act
        var result = (await _service.GetAllMappingsAsync()).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].ReaderId, Is.EqualTo(Guid.Parse(mappings[0].ReaderId)));
        Assert.That(result[0].PluginId, Is.EqualTo(mappings[0].PluginId));
        Assert.That(result[0].ExecutionOrder, Is.EqualTo(mappings[0].ExecutionOrder));
        Assert.That(result[0].IsEnabled, Is.EqualTo(mappings[0].IsEnabled));
    }

    [Test]
    public async Task CopyMappingsAsync_CopiesAllMappingsToTarget()
    {
        // Arrange
        var sourceReaderId = Guid.NewGuid();
        var targetReaderId = Guid.NewGuid();
        var sourceMappings = new List<ReaderPluginMappingEntity>
        {
            new() { ReaderId = sourceReaderId.ToString(), PluginId = "Plugin1", ExecutionOrder = 1, IsEnabled = true },
            new() { ReaderId = sourceReaderId.ToString(), PluginId = "Plugin2", ExecutionOrder = 2, IsEnabled = false }
        };

        _mockRepository.Setup(r => r.GetMappingsForReaderAsync(sourceReaderId))
            .ReturnsAsync(sourceMappings);

        // Act
        await _service.CopyMappingsAsync(sourceReaderId, targetReaderId);

        // Assert
        _mockRepository.Verify(r => r.DeleteMappingsForReaderAsync(targetReaderId), Times.Once);
        
        _mockRepository.Verify(r => r.CreateMappingAsync(It.Is<ReaderPluginMappingEntity>(m =>
            m.ReaderId == targetReaderId.ToString() &&
            m.PluginId == "Plugin1" &&
            m.ExecutionOrder == 1 &&
            m.IsEnabled == true)), Times.Once);
            
        _mockRepository.Verify(r => r.CreateMappingAsync(It.Is<ReaderPluginMappingEntity>(m =>
            m.ReaderId == targetReaderId.ToString() &&
            m.PluginId == "Plugin2" &&
            m.ExecutionOrder == 2 &&
            m.IsEnabled == false)), Times.Once);
    }

    [Test]
    public async Task GetReadersUsingPluginAsync_ReturnsEnabledReadersOnly()
    {
        // Arrange
        var pluginId = "TestPlugin";
        var readerId1 = Guid.NewGuid();
        var readerId2 = Guid.NewGuid();
        var readerId3 = Guid.NewGuid();
        
        var mappings = new List<ReaderPluginMappingEntity>
        {
            new() { ReaderId = readerId1.ToString(), PluginId = pluginId, IsEnabled = true },
            new() { ReaderId = readerId2.ToString(), PluginId = pluginId, IsEnabled = false },
            new() { ReaderId = readerId3.ToString(), PluginId = pluginId, IsEnabled = true }
        };

        _mockRepository.Setup(r => r.GetMappingsForPluginAsync(pluginId))
            .ReturnsAsync(mappings);

        // Act
        var result = (await _service.GetReadersUsingPluginAsync(pluginId)).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(2)); // Only enabled mappings
        Assert.That(result.Contains(readerId1), Is.True);
        Assert.That(result.Contains(readerId2), Is.False);
        Assert.That(result.Contains(readerId3), Is.True);
    }

    [Test]
    public async Task GetReadersUsingPluginAsync_ReturnsDistinctReaders()
    {
        // Arrange
        var pluginId = "TestPlugin";
        var readerId = Guid.NewGuid();
        
        // Duplicate mappings for same reader (shouldn't happen but test anyway)
        var mappings = new List<ReaderPluginMappingEntity>
        {
            new() { ReaderId = readerId.ToString(), PluginId = pluginId, IsEnabled = true },
            new() { ReaderId = readerId.ToString(), PluginId = pluginId, IsEnabled = true }
        };

        _mockRepository.Setup(r => r.GetMappingsForPluginAsync(pluginId))
            .ReturnsAsync(mappings);

        // Act
        var result = (await _service.GetReadersUsingPluginAsync(pluginId)).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(1)); // Distinct readers only
        Assert.That(result[0], Is.EqualTo(readerId));
    }
}