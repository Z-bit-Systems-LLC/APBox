using ApBox.Core.Data.Models;
using ApBox.Core.Models;
using ApBox.Plugins;
using ApBox.Web.Hubs;
using ApBox.Web.ViewModels;
using ApBox.Web.Models.Notifications;
using ApBox.Web.Services.Notifications;
using Moq;

namespace ApBox.Web.Tests.ViewModels;

/// <summary>
/// Tests for DashboardViewModel
/// </summary>
[TestFixture]
[Category("Unit")]
public class DashboardViewModelTests : ApBoxTestContext
{
    private Mock<INotificationAggregator> _mockNotificationAggregator;
    private DashboardViewModel _viewModel;
    private Action<CardEventNotification>? _cardEventHandler;
    private Action<PinEventNotification>? _pinEventHandler;
    private Action<ReaderStatusNotification>? _readerStatusHandler;

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
        
        // Create mock notification aggregator for this test
        _mockNotificationAggregator = new Mock<INotificationAggregator>();
        
        // Capture subscription handlers
        _mockNotificationAggregator
            .Setup(x => x.Subscribe<CardEventNotification>(It.IsAny<Action<CardEventNotification>>()))
            .Callback<Action<CardEventNotification>>(handler => _cardEventHandler = handler);
        
        _mockNotificationAggregator
            .Setup(x => x.Subscribe<PinEventNotification>(It.IsAny<Action<PinEventNotification>>()))
            .Callback<Action<PinEventNotification>>(handler => _pinEventHandler = handler);
            
        _mockNotificationAggregator
            .Setup(x => x.Subscribe<ReaderStatusNotification>(It.IsAny<Action<ReaderStatusNotification>>()))
            .Callback<Action<ReaderStatusNotification>>(handler => _readerStatusHandler = handler);

        // Create ViewModel with mock notification aggregator
        _viewModel = new DashboardViewModel(
            MockReaderService.Object,
            MockPluginLoader.Object,
            MockCardEventRepository.Object,
            MockPinEventRepository.Object,
            _mockNotificationAggregator.Object);
            
        // Set up UI callbacks
        _viewModel.StateHasChanged = () => { };
        _viewModel.InvokeAsync = func => func();
    }

    [TearDown]
    public void TearDown()
    {
        _viewModel?.Dispose();
    }

    // ==============================================
    // Card Event Notification Tests
    // ==============================================

    [Test]
    public async Task InitializeAsync_ShouldCompleteSuccessfully()
    {
        // Act
        await _viewModel.InitializeAsync();

        // Assert
        Assert.That(_viewModel.IsLoading, Is.False);
        Assert.That(_viewModel.ErrorMessage, Is.Null);
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
            Timestamp = DateTime.UtcNow
        };

        // Act
        _cardEventHandler?.Invoke(cardEvent);

        // Assert
        Assert.That(_viewModel.RecentEvents.Count, Is.EqualTo(1), "Should have exactly one event after processing");
        var addedEvent = _viewModel.RecentEvents.First();
        Assert.That(addedEvent, Is.TypeOf<CardEventDisplay>(), "Added event should be a CardEventDisplay");
        var cardEventDisplay = (CardEventDisplay)addedEvent;
        Assert.That(cardEventDisplay.ReaderId, Is.EqualTo(cardEvent.ReaderId));
        Assert.That(cardEventDisplay.ReaderName, Is.EqualTo(cardEvent.ReaderName));
        Assert.That(cardEventDisplay.CardNumber, Is.EqualTo(cardEvent.CardNumber));
        Assert.That(cardEventDisplay.BitLength, Is.EqualTo(cardEvent.BitLength));
        Assert.That(cardEventDisplay.Timestamp, Is.EqualTo(cardEvent.Timestamp));
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
            Timestamp = DateTime.UtcNow
        };

        // Act
        _cardEventHandler?.Invoke(cardEvent);

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
                Timestamp = DateTime.UtcNow.AddMinutes(-i)
            };

            // Act
            _cardEventHandler?.Invoke(cardEvent);
        }

        // Assert
        Assert.That(_viewModel.RecentEvents.Count, Is.EqualTo(25));
        
        // Verify most recent event is first
        var firstEvent = (CardEventDisplay)_viewModel.RecentEvents.First();
        Assert.That(firstEvent.ReaderName, Is.EqualTo("Reader 29"));
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
            Timestamp = DateTime.UtcNow.AddMinutes(-5)
        };
        
        var event2 = new CardEventNotification
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "Second Reader", 
            CardNumber = "222222222",
            BitLength = 26,
            Timestamp = DateTime.UtcNow
        };

        // Act
        _cardEventHandler?.Invoke(event1);
        _cardEventHandler?.Invoke(event2);

        // Assert
        Assert.That(_viewModel.RecentEvents.Count, Is.EqualTo(2), "Should have exactly two events after processing both");
        var firstEvent = (CardEventDisplay)_viewModel.RecentEvents[0];
        var secondEvent = (CardEventDisplay)_viewModel.RecentEvents[1];
        Assert.That(firstEvent.ReaderName, Is.EqualTo("Second Reader")); // Most recent first
        Assert.That(secondEvent.ReaderName, Is.EqualTo("First Reader"));
    }

    // ==============================================
    // Reader Status Notification Tests
    // ==============================================


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
        _readerStatusHandler?.Invoke(statusNotification);

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
        _readerStatusHandler?.Invoke(onlineNotification);
        
        // Then set reader offline
        var offlineNotification = new ReaderStatusNotification
        {
            ReaderId = readerId,
            ReaderName = "Test Reader",
            IsOnline = false
        };

        // Act
        _readerStatusHandler?.Invoke(offlineNotification);

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
        _readerStatusHandler?.Invoke(reader1Notification);
        _readerStatusHandler?.Invoke(reader2Notification);

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
            Timestamp = DateTime.UtcNow
        };

        // Act
        _cardEventHandler?.Invoke(cardEvent);

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
        _readerStatusHandler?.Invoke(statusNotification);

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
            Timestamp = DateTime.UtcNow
        };

        // Act
        _cardEventHandler?.Invoke(cardEvent);

        // Assert
        Assert.That(invokeAsyncCalled, Is.True, "InvokeAsync should be called for thread safety");
    }
}