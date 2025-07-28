using ApBox.Core.Data.Models;
using ApBox.Core.Models;
using ApBox.Plugins;
using ApBox.Web.Hubs;
using ApBox.Web.Services;
using ApBox.Web.ViewModels;
using Microsoft.AspNetCore.SignalR.Client;

namespace ApBox.Web.Tests.ViewModels;

/// <summary>
/// Tests for DashboardViewModel
/// </summary>
[TestFixture]
[Category("Unit")]
public class DashboardViewModelTests : ApBoxTestContext
{
    private Mock<IHubConnectionWrapper> _mockHubConnection;
    private DashboardViewModel _viewModel;
    private Dictionary<string, Func<object, Task>> _eventHandlers;

    [SetUp]
    public void Setup()
    {
        ResetMocks();
        
        // Setup empty collections for clean testing state
        MockReaderService.Setup(x => x.GetReadersAsync())
            .ReturnsAsync(new List<ReaderConfiguration>());
        MockReaderService.Setup(x => x.GetAllReaderStatusesAsync())
            .ReturnsAsync(new Dictionary<Guid, bool>());
        MockPluginLoader.Setup(x => x.LoadPluginsAsync())
            .ReturnsAsync(new List<IApBoxPlugin>());
        MockCardEventRepository.Setup(x => x.GetRecentAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<CardEventEntity>());
        MockCardEventRepository.Setup(x => x.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>()))
            .ReturnsAsync(new List<CardEventEntity>());
        
        // Create a dedicated mock for this test
        _mockHubConnection = new Mock<IHubConnectionWrapper>();
        _eventHandlers = new Dictionary<string, Func<object, Task>>();
        
        // Setup the On method to capture event handlers
        _mockHubConnection.Setup(x => x.On<CardEventNotification>(It.IsAny<string>(), It.IsAny<Func<CardEventNotification, Task>>()))
            .Returns<string, Func<CardEventNotification, Task>>((methodName, handler) =>
            {
                _eventHandlers[methodName] = async (data) => await handler((CardEventNotification)data);
                return Mock.Of<IDisposable>();
            });
            
        _mockHubConnection.Setup(x => x.On<ReaderStatusNotification>(It.IsAny<string>(), It.IsAny<Func<ReaderStatusNotification, Task>>()))
            .Returns<string, Func<ReaderStatusNotification, Task>>((methodName, handler) =>
            {
                _eventHandlers[methodName] = async (data) => await handler((ReaderStatusNotification)data);
                return Mock.Of<IDisposable>();
            });

        _mockHubConnection.Setup(x => x.StartAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
            
        _mockHubConnection.Setup(x => x.State)
            .Returns(HubConnectionState.Disconnected);

        // Create ViewModel with mocked hub connection
        _viewModel = new DashboardViewModel(
            MockReaderService.Object,
            MockPluginLoader.Object,
            MockCardEventRepository.Object,
            _mockHubConnection.Object);
            
        // Set up UI callbacks
        _viewModel.StateHasChanged = () => { };
        _viewModel.InvokeAsync = func => func();
    }

    [TearDown]
    public void TearDown()
    {
        _viewModel?.DisposeAsync().AsTask().Wait();
    }

    // ==============================================
    // Card Event Notification Tests
    // ==============================================

    [Test]
    public async Task InitializeAsync_ShouldRegisterCardEventHandler()
    {
        // Act
        await _viewModel.InitializeAsync();

        // Assert
        _mockHubConnection.Verify(x => x.On<CardEventNotification>("CardEventProcessed", It.IsAny<Func<CardEventNotification, Task>>()), Times.Once);
        Assert.That(_eventHandlers.ContainsKey("CardEventProcessed"), Is.True);
    }

    [Test]
    public async Task CardEventProcessed_ShouldAddEventToRecentEvents()
    {
        // Arrange
        await _viewModel.InitializeAsync();
        
        var cardEvent = new CardEventNotification
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "Test Reader",
            CardNumber = "123456789",
            BitLength = 26,
            Timestamp = DateTime.Now
        };

        // Act
        await _eventHandlers["CardEventProcessed"](cardEvent);

        // Assert
        Assert.That(_viewModel.RecentEvents.Count, Is.EqualTo(1), "Should have exactly one event after processing");
        var addedEvent = _viewModel.RecentEvents.First();
        Assert.That(addedEvent.ReaderId, Is.EqualTo(cardEvent.ReaderId));
        Assert.That(addedEvent.ReaderName, Is.EqualTo(cardEvent.ReaderName));
        Assert.That(addedEvent.CardNumber, Is.EqualTo(cardEvent.CardNumber));
        Assert.That(addedEvent.BitLength, Is.EqualTo(cardEvent.BitLength));
        Assert.That(addedEvent.Timestamp, Is.EqualTo(cardEvent.Timestamp));
    }

    [Test]
    public async Task CardEventProcessed_ShouldIncrementTotalEvents()
    {
        // Arrange
        await _viewModel.InitializeAsync();
        var initialCount = _viewModel.TotalEvents;
        
        var cardEvent = new CardEventNotification
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "Test Reader",
            CardNumber = "123456789",
            BitLength = 26,
            Timestamp = DateTime.Now
        };

        // Act
        await _eventHandlers["CardEventProcessed"](cardEvent);

        // Assert
        Assert.That(_viewModel.TotalEvents, Is.EqualTo(initialCount + 1));
    }

    [Test]
    public async Task CardEventProcessed_ShouldMaintainMaximum25Events()
    {
        // Arrange
        await _viewModel.InitializeAsync();
        
        // Add 30 events to test the limit
        for (int i = 0; i < 30; i++)
        {
            var cardEvent = new CardEventNotification
            {
                ReaderId = Guid.NewGuid(),
                ReaderName = $"Reader {i}",
                CardNumber = $"123456{i:D3}",
                BitLength = 26,
                Timestamp = DateTime.Now.AddMinutes(-i)
            };

            // Act
            await _eventHandlers["CardEventProcessed"](cardEvent);
        }

        // Assert
        Assert.That(_viewModel.RecentEvents.Count, Is.EqualTo(25));
        
        // Verify most recent event is first
        Assert.That(_viewModel.RecentEvents.First().ReaderName, Is.EqualTo("Reader 29"));
    }

    [Test]
    public async Task CardEventProcessed_ShouldAddEventsInCorrectOrder()
    {
        // Arrange
        await _viewModel.InitializeAsync();
        
        var event1 = new CardEventNotification
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "First Reader",
            CardNumber = "111111111",
            BitLength = 26,
            Timestamp = DateTime.Now.AddMinutes(-5)
        };
        
        var event2 = new CardEventNotification
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "Second Reader", 
            CardNumber = "222222222",
            BitLength = 26,
            Timestamp = DateTime.Now
        };

        // Act
        await _eventHandlers["CardEventProcessed"](event1);
        await _eventHandlers["CardEventProcessed"](event2);

        // Assert
        Assert.That(_viewModel.RecentEvents.Count, Is.EqualTo(2), "Should have exactly two events after processing both");
        Assert.That(_viewModel.RecentEvents[0].ReaderName, Is.EqualTo("Second Reader")); // Most recent first
        Assert.That(_viewModel.RecentEvents[1].ReaderName, Is.EqualTo("First Reader"));
    }

    // ==============================================
    // Reader Status Notification Tests
    // ==============================================

    [Test]
    public async Task InitializeAsync_ShouldRegisterReaderStatusHandler()
    {
        // Act
        await _viewModel.InitializeAsync();

        // Assert
        _mockHubConnection.Verify(x => x.On<ReaderStatusNotification>("ReaderStatusChanged", It.IsAny<Func<ReaderStatusNotification, Task>>()), Times.Once);
        Assert.That(_eventHandlers.ContainsKey("ReaderStatusChanged"), Is.True);
    }

    [Test]
    public async Task ReaderStatusChanged_ShouldUpdateReaderStatus()
    {
        // Arrange
        await _viewModel.InitializeAsync();
        var readerId = Guid.NewGuid();
        
        var statusNotification = new ReaderStatusNotification
        {
            ReaderId = readerId,
            ReaderName = "Test Reader",
            IsOnline = true
        };

        // Act
        await _eventHandlers["ReaderStatusChanged"](statusNotification);

        // Assert
        Assert.That(_viewModel.ReaderStatuses.ContainsKey(readerId), Is.True);
        Assert.That(_viewModel.ReaderStatuses[readerId], Is.True);
    }

    [Test]
    public async Task ReaderStatusChanged_OfflineStatus_ShouldUpdateCorrectly()
    {
        // Arrange
        await _viewModel.InitializeAsync();
        var readerId = Guid.NewGuid();
        
        // First set reader online
        var onlineNotification = new ReaderStatusNotification
        {
            ReaderId = readerId,
            ReaderName = "Test Reader",
            IsOnline = true
        };
        await _eventHandlers["ReaderStatusChanged"](onlineNotification);
        
        // Then set reader offline
        var offlineNotification = new ReaderStatusNotification
        {
            ReaderId = readerId,
            ReaderName = "Test Reader",
            IsOnline = false
        };

        // Act
        await _eventHandlers["ReaderStatusChanged"](offlineNotification);

        // Assert
        Assert.That(_viewModel.ReaderStatuses.ContainsKey(readerId), Is.True);
        Assert.That(_viewModel.ReaderStatuses[readerId], Is.False);
    }

    [Test]
    public async Task ReaderStatusChanged_MultipleReaders_ShouldUpdateIndividually()
    {
        // Arrange
        await _viewModel.InitializeAsync();
        var reader1Id = Guid.NewGuid();
        var reader2Id = Guid.NewGuid();
        
        var reader1Notification = new ReaderStatusNotification
        {
            ReaderId = reader1Id,
            ReaderName = "Reader 1",
            IsOnline = true
        };
        
        var reader2Notification = new ReaderStatusNotification
        {
            ReaderId = reader2Id,
            ReaderName = "Reader 2",
            IsOnline = false
        };

        // Act
        await _eventHandlers["ReaderStatusChanged"](reader1Notification);
        await _eventHandlers["ReaderStatusChanged"](reader2Notification);

        // Assert
        Assert.That(_viewModel.ReaderStatuses.Count, Is.EqualTo(2));
        
        Assert.That(_viewModel.ReaderStatuses.ContainsKey(reader1Id), Is.True);
        Assert.That(_viewModel.ReaderStatuses[reader1Id], Is.True);
        
        Assert.That(_viewModel.ReaderStatuses.ContainsKey(reader2Id), Is.True);
        Assert.That(_viewModel.ReaderStatuses[reader2Id], Is.False);
    }

    // ==============================================
    // Event Handler Management Tests
    // ==============================================

    [Test]
    public async Task InitializeAsync_ShouldStartHubConnection()
    {
        // Act
        await _viewModel.InitializeAsync();

        // Assert
        _mockHubConnection.Verify(x => x.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task MultipleInitialize_RegistersHandlersMultipleTimes()
    {
        // Act
        await _viewModel.InitializeAsync();
        await _viewModel.InitializeAsync(); // Call twice

        // Assert - Current implementation doesn't prevent multiple registrations
        _mockHubConnection.Verify(x => x.On<CardEventNotification>("CardEventProcessed", It.IsAny<Func<CardEventNotification, Task>>()), Times.Exactly(2));
        _mockHubConnection.Verify(x => x.On<ReaderStatusNotification>("ReaderStatusChanged", It.IsAny<Func<ReaderStatusNotification, Task>>()), Times.Exactly(2));
    }

    // ==============================================
    // UI Update Integration Tests
    // ==============================================

    [Test]
    public async Task SignalRMessage_ShouldTriggerUIUpdate()
    {
        // Arrange
        await _viewModel.InitializeAsync();
        bool stateChangedCalled = false;
        _viewModel.StateHasChanged = () => stateChangedCalled = true;
        
        var cardEvent = new CardEventNotification
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "Test Reader",
            CardNumber = "123456789",
            BitLength = 26,
            Timestamp = DateTime.Now
        };

        // Act
        await _eventHandlers["CardEventProcessed"](cardEvent);

        // Assert
        Assert.That(stateChangedCalled, Is.True, "StateHasChanged should be called to update UI");
    }

    [Test]
    public async Task ReaderStatusMessage_ShouldTriggerUIUpdate()
    {
        // Arrange
        await _viewModel.InitializeAsync();
        bool stateChangedCalled = false;
        _viewModel.StateHasChanged = () => stateChangedCalled = true;
        
        var statusNotification = new ReaderStatusNotification
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "Test Reader",
            IsOnline = true
        };

        // Act
        await _eventHandlers["ReaderStatusChanged"](statusNotification);

        // Assert
        Assert.That(stateChangedCalled, Is.True, "StateHasChanged should be called to update UI");
    }

    [Test]
    public async Task SignalRMessage_ShouldInvokeAsync()
    {
        // Arrange
        await _viewModel.InitializeAsync();
        bool invokeAsyncCalled = false;
        _viewModel.InvokeAsync = func => 
        {
            invokeAsyncCalled = true;
            return func();
        };
        
        var cardEvent = new CardEventNotification
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "Test Reader",
            CardNumber = "123456789",
            BitLength = 26,
            Timestamp = DateTime.Now
        };

        // Act
        await _eventHandlers["CardEventProcessed"](cardEvent);

        // Assert
        Assert.That(invokeAsyncCalled, Is.True, "InvokeAsync should be called for thread safety");
    }
}