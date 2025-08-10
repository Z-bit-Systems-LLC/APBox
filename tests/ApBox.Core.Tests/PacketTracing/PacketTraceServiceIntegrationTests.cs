using ApBox.Core.PacketTracing.Services;
using ApBox.Core.PacketTracing.Models;
using ApBox.Core.PacketTracing;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using OSDP.Net.Tracing;
using OSDP.Net.Model;
using System.Reflection;

namespace ApBox.Core.Tests.PacketTracing;

[TestFixture]
[Category("Integration")]
public class PacketTraceServiceIntegrationTests
{
    private PacketTraceService _service;
    private ILogger<PacketTraceService> _logger;

    [SetUp]
    public void Setup()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<PacketTraceService>();
        _service = new PacketTraceService(null); // No reader service needed for tests
    }

    [Test]
    public void StartTracing_ForReader_EnablesTracingForReader()
    {
        // Arrange
        var readerId = Guid.NewGuid().ToString();
        
        // Act
        _service.StartTracing(readerId);
        
        // Assert
        Assert.That(_service.IsTracingReader(readerId), Is.True);
        Assert.That(_service.IsTracing, Is.True);
    }

    [Test]
    public void StopTracing_ForReader_DisablesTracingForReader()
    {
        // Arrange
        var readerId = Guid.NewGuid().ToString();
        _service.StartTracing(readerId);
        
        // Act
        _service.StopTracing(readerId);
        
        // Assert
        Assert.That(_service.IsTracingReader(readerId), Is.False);
        Assert.That(_service.IsTracing, Is.False);
    }

    [Test]
    public void UpdateSettings_UpdatesServiceSettings()
    {
        // Arrange
        var settings = new PacketTraceSettings
        {
            Enabled = true,
            MemoryLimitMB = 20,
            FilterPollCommands = true,
            FilterAckCommands = false
        };
        
        // Act
        _service.UpdateSettings(settings);
        
        // Assert
        var currentSettings = _service.GetCurrentSettings();
        Assert.That(currentSettings.Enabled, Is.EqualTo(settings.Enabled));
        Assert.That(currentSettings.MemoryLimitMB, Is.EqualTo(settings.MemoryLimitMB));
        Assert.That(currentSettings.FilterPollCommands, Is.EqualTo(settings.FilterPollCommands));
        Assert.That(currentSettings.FilterAckCommands, Is.EqualTo(settings.FilterAckCommands));
    }

    [Test]
    public void GetTraces_WithNoTraces_ReturnsEmptyList()
    {
        // Arrange
        var readerId = Guid.NewGuid().ToString();
        
        // Act
        var traces = _service.GetTraces(readerId).ToList();
        
        // Assert
        Assert.That(traces, Is.Empty);
    }

    [Test]
    public void GetStatistics_ReturnsValidStatistics()
    {
        // Arrange
        var readerId = Guid.NewGuid().ToString();
        _service.StartTracing(readerId);
        
        // Act
        var stats = _service.GetStatistics();
        
        // Assert
        Assert.That(stats, Is.Not.Null);
        Assert.That(stats.ReplyPercentage, Is.GreaterThanOrEqualTo(0));
        Assert.That(stats.ReplyPercentage, Is.LessThanOrEqualTo(100));
    }

    [Test]
    public void GetStatistics_ReplyPercentage_StartsAtZero()
    {
        // Arrange
        var readerId = Guid.NewGuid().ToString();
        _service.StartTracing(readerId);
        
        // Act
        var stats = _service.GetStatistics();
        
        // Assert - No packets captured, should be 0%
        Assert.That(stats.ReplyPercentage, Is.EqualTo(0.0));
        Assert.That(stats.TotalOutgoingPackets, Is.EqualTo(0));
        Assert.That(stats.PacketsWithReplies, Is.EqualTo(0));
    }
    

    [TearDown]
    public void TearDown()
    {
        _service.StopTracingAll();
        _service.ClearTraces();
    }
}