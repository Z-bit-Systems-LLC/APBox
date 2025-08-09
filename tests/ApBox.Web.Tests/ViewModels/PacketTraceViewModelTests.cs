using ApBox.Core.PacketTracing.Services;
using ApBox.Core.PacketTracing.Models;
using ApBox.Core.PacketTracing;
using ApBox.Web.ViewModels;
using ApBox.Web.Services.Notifications;
using Blazored.LocalStorage;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ApBox.Web.Tests.ViewModels;

[TestFixture]
[Category("Unit")]
public class PacketTraceViewModelTests
{
    private PacketTraceViewModel _viewModel;
    private Mock<IPacketTraceService> _mockPacketTraceService;
    private Mock<ILocalStorageService> _mockLocalStorage;
    private Mock<INotificationAggregator> _mockNotificationAggregator;

    [SetUp]
    public void Setup()
    {
        _mockPacketTraceService = new Mock<IPacketTraceService>();
        _mockLocalStorage = new Mock<ILocalStorageService>();
        _mockNotificationAggregator = new Mock<INotificationAggregator>();

        _viewModel = new PacketTraceViewModel(
            _mockPacketTraceService.Object,
            _mockLocalStorage.Object,
            _mockNotificationAggregator.Object);
    }

    [Test]
    public async Task InitializeAsync_LoadsExistingTraces()
    {
        // Arrange
        var existingTraces = new List<PacketTraceEntry>
        {
            CreateTestPacketTrace("Reader1", PacketDirection.Incoming),
            CreateTestPacketTrace("Reader2", PacketDirection.Outgoing)
        };

        _mockPacketTraceService.Setup(s => s.GetTraces(null, 100, true, false))
                              .Returns(existingTraces);

        var mockStats = new TracingStatistics
        {
            TotalPackets = 2,
            FilteredPackets = 0,
            MemoryUsageBytes = 1024
        };

        _mockPacketTraceService.Setup(s => s.GetStatistics())
                              .Returns(mockStats);

        // Act
        await _viewModel.InitializeAsync();

        // Assert
        Assert.That(_viewModel.Packets.Count, Is.EqualTo(2));
        Assert.That(_viewModel.TotalPackets, Is.EqualTo(2));
        Assert.That(_viewModel.FilteredPackets, Is.EqualTo(0));
    }

    [Test]
    public async Task ApplySettingsAsync_UpdatesServiceSettings()
    {
        // Arrange
        _viewModel.TracingEnabled = true;
        _viewModel.MaxPacketsPerReader = 1000;
        _viewModel.MaxAgeMinutes = 30;
        _viewModel.FilterPollCommands = false;
        _viewModel.FilterAckCommands = true;
        _viewModel.LimitMode = TraceLimitMode.Hybrid;

        PacketTraceSettings? appliedSettings = null;
        _mockPacketTraceService.Setup(s => s.UpdateSettings(It.IsAny<PacketTraceSettings>()))
                              .Callback<PacketTraceSettings>(settings => appliedSettings = settings);

        // Act
        await _viewModel.ApplySettingsCommand.ExecuteAsync(null);

        // Assert
        Assert.That(appliedSettings, Is.Not.Null);
        Assert.That(appliedSettings.Enabled, Is.True);
        Assert.That(appliedSettings.MaxPacketsPerReader, Is.EqualTo(1000));
        Assert.That(appliedSettings.MaxAgeMinutes, Is.EqualTo(30));
        Assert.That(appliedSettings.FilterPollCommands, Is.False);
        Assert.That(appliedSettings.FilterAckCommands, Is.True);
        Assert.That(appliedSettings.LimitMode, Is.EqualTo(TraceLimitMode.Hybrid));
    }

    [Test]
    public void StartTracing_CallsServiceStartTracingAll()
    {
        // Act
        _viewModel.StartTracingCommand.Execute(null);

        // Assert
        _mockPacketTraceService.Verify(s => s.StartTracingAll(), Times.Once);
        Assert.That(_viewModel.TracingEnabled, Is.True);
    }

    [Test]
    public void StopTracing_CallsServiceStopTracingAll()
    {
        // Arrange
        _viewModel.TracingEnabled = true;

        // Act
        _viewModel.StopTracingCommand.Execute(null);

        // Assert
        _mockPacketTraceService.Verify(s => s.StopTracingAll(), Times.Once);
        Assert.That(_viewModel.TracingEnabled, Is.False);
    }

    [Test]
    public void ClearAllTraces_CallsServiceClearAndUpdatesUI()
    {
        // Arrange
        _viewModel.Packets.Add(CreateTestPacketTrace("Reader1", PacketDirection.Incoming));
        _viewModel.Packets.Add(CreateTestPacketTrace("Reader2", PacketDirection.Outgoing));

        var mockStats = new TracingStatistics
        {
            TotalPackets = 0,
            FilteredPackets = 0,
            MemoryUsageBytes = 0
        };

        _mockPacketTraceService.Setup(s => s.GetStatistics())
                              .Returns(mockStats);

        // Act
        _viewModel.ClearAllTracesCommand.Execute(null);

        // Assert
        _mockPacketTraceService.Verify(s => s.ClearTraces(null), Times.Once);
        Assert.That(_viewModel.Packets.Count, Is.EqualTo(0));
        Assert.That(_viewModel.TotalPackets, Is.EqualTo(0));
    }

    [Test]
    public async Task InitializeWithJavaScriptAsync_LoadsSettingsFromLocalStorage()
    {
        // Arrange
        var savedSettings = new PacketTraceSettings
        {
            Enabled = true,
            MaxPacketsPerReader = 750,
            MaxAgeMinutes = 20,
            FilterPollCommands = false,
            FilterAckCommands = true,
            LimitMode = TraceLimitMode.Time
        };

        _mockLocalStorage.Setup(s => s.ContainKeyAsync("packetTraceSettings", default))
                        .ReturnsAsync(true);
        _mockLocalStorage.Setup(s => s.GetItemAsync<PacketTraceSettings>("packetTraceSettings", default))
                        .ReturnsAsync(savedSettings);

        // Act
        await _viewModel.InitializeWithJavaScriptAsync();

        // Assert
        Assert.That(_viewModel.TracingEnabled, Is.True);
        Assert.That(_viewModel.MaxPacketsPerReader, Is.EqualTo(750));
        Assert.That(_viewModel.MaxAgeMinutes, Is.EqualTo(20));
        Assert.That(_viewModel.FilterPollCommands, Is.False);
        Assert.That(_viewModel.FilterAckCommands, Is.True);
        Assert.That(_viewModel.LimitMode, Is.EqualTo(TraceLimitMode.Time));
    }

    [Test]
    public void InitializeWithJavaScriptAsync_HandlesLocalStorageErrors()
    {
        // Arrange
        _mockLocalStorage.Setup(s => s.ContainKeyAsync("packetTraceSettings", default))
                        .ThrowsAsync(new InvalidOperationException("LocalStorage not available"));

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _viewModel.InitializeWithJavaScriptAsync());
    }

    [Test]
    public void PacketCapturedEvent_UpdatesUICollection()
    {
        // Arrange
        var testPacket = CreateTestPacketTrace("TestReader", PacketDirection.Incoming);
        
        // Setup mock statistics
        var mockStats = new TracingStatistics
        {
            TotalPackets = 1,
            FilteredPackets = 0,
            MemoryUsageBytes = 1024
        };
        _mockPacketTraceService.Setup(s => s.GetStatistics()).Returns(mockStats);

        // Simulate the service raising the PacketCaptured event
        _mockPacketTraceService.Raise(s => s.PacketCaptured += null, _mockPacketTraceService.Object, testPacket);

        // Assert
        Assert.That(_viewModel.Packets.Count, Is.EqualTo(1));
        Assert.That(_viewModel.Packets.First(), Is.EqualTo(testPacket));
    }

    [Test]
    public void PacketCapturedEvent_LimitsUICollectionTo100()
    {
        // Arrange - Fill with 100 packets
        for (int i = 0; i < 100; i++)
        {
            var packet = CreateTestPacketTrace($"Reader{i}", PacketDirection.Incoming);
            _viewModel.Packets.Add(packet);
        }

        var newPacket = CreateTestPacketTrace("NewReader", PacketDirection.Outgoing);
        
        // Setup mock statistics
        var mockStats = new TracingStatistics
        {
            TotalPackets = 101,
            FilteredPackets = 0,
            MemoryUsageBytes = 1024
        };
        _mockPacketTraceService.Setup(s => s.GetStatistics()).Returns(mockStats);

        // Act - Add one more packet (should trigger the limit)
        _mockPacketTraceService.Raise(s => s.PacketCaptured += null, _mockPacketTraceService.Object, newPacket);

        // Assert
        Assert.That(_viewModel.Packets.Count, Is.EqualTo(100)); // Should still be 100
        Assert.That(_viewModel.Packets.First(), Is.EqualTo(newPacket)); // New packet should be first
    }

    [Test]
    public void ExportToOsdpCapAsync_ThrowsNotImplementedException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<NotImplementedException>(
            async () => await _viewModel.ExportToOsdpCapCommand.ExecuteAsync(null));
        
        Assert.That(ex.Message, Does.Contain("OSDPCAP export"));
    }

    private static PacketTraceEntry CreateTestPacketTrace(string readerName, PacketDirection direction)
    {
        var rawData = new byte[] { 0x53, 0x01, 0x08, 0x00, 0x40, 0x00, 0x1C, 0x7B };
        return PacketTraceEntry.Create(
            rawData,
            direction,
            Guid.NewGuid().ToString(),
            readerName,
            0x01,
            null);
    }
}