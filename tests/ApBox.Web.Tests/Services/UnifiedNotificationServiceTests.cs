using ApBox.Core.Models;
using ApBox.Core.PacketTracing.Services;
using ApBox.Core.Services.Core;
using ApBox.Core.Services.Events;
using ApBox.Plugins;
using ApBox.Web.Hubs;
using ApBox.Web.Models.Notifications;
using ApBox.Web.Services.Notifications;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ApBox.Web.Tests.Services;

[TestFixture]
public class UnifiedNotificationServiceTests
{
    private Mock<IEventPublisher> _mockEventPublisher = null!;
    private Mock<IHubContext<NotificationHub, INotificationClient>> _mockHubContext = null!;
    private Mock<IPacketTraceService> _mockPacketTraceService = null!;
    private Mock<ILogger<UnifiedNotificationService>> _mockLogger = null!;
    private Mock<INotificationClient> _mockNotificationClient = null!;
    private Mock<IHubCallerClients<INotificationClient>> _mockClients = null!;
    private UnifiedNotificationService _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockHubContext = new Mock<IHubContext<NotificationHub, INotificationClient>>();
        _mockPacketTraceService = new Mock<IPacketTraceService>();
        _mockLogger = new Mock<ILogger<UnifiedNotificationService>>();
        _mockNotificationClient = new Mock<INotificationClient>();
        _mockClients = new Mock<IHubCallerClients<INotificationClient>>();
        
        _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);
        _mockClients.Setup(c => c.All).Returns(_mockNotificationClient.Object);

        _service = new UnifiedNotificationService(
            _mockEventPublisher.Object,
            _mockHubContext.Object,
            _mockPacketTraceService.Object,
            _mockLogger.Object);
    }

    [Test]
    [Category("Unit")]
    public async Task OnPinProcessingCompleted_ShouldIncludePinLengthAndCompletionReason()
    {
        // Arrange
        var readerId = Guid.NewGuid();
        var readerName = "Test Reader";
        var pin = "1234";
        var completionReason = PinCompletionReason.PoundKey;
        var timestamp = DateTime.UtcNow;

        var pinReadEvent = new PinReadEvent
        {
            ReaderId = readerId,
            ReaderName = readerName,
            Pin = pin,
            CompletionReason = completionReason,
            Timestamp = timestamp
        };

        var result = new PinReadResult
        {
            Success = true,
            Message = "PIN accepted"
        };

        var feedback = new ReaderFeedback
        {
            Type = ReaderFeedbackType.Success
        };

        var completedEvent = new PinProcessingCompletedEvent
        {
            PinRead = pinReadEvent,
            Result = result,
            Feedback = feedback,
            PersistenceSuccessful = true,
            FeedbackDeliverySuccessful = true
        };

        // Capture the PinEventNotification sent to SignalR
        PinEventNotification? capturedNotification = null;
        _mockNotificationClient
            .Setup(c => c.PinEventProcessed(It.IsAny<PinEventNotification>()))
            .Callback<PinEventNotification>(notification => capturedNotification = notification)
            .Returns(Task.CompletedTask);

        // Act
        await _service.StartAsync(CancellationToken.None);
        
        // Get the handler that was registered for PinProcessingCompletedEvent
        _mockEventPublisher.Verify(p => p.Subscribe<PinProcessingCompletedEvent>(It.IsAny<Func<PinProcessingCompletedEvent, Task>>()));
        
        // Simulate the event being raised
        var subscribedHandler = GetSubscribedHandler<PinProcessingCompletedEvent>();
        await subscribedHandler(completedEvent);

        // Assert
        Assert.That(capturedNotification, Is.Not.Null, "PinEventNotification should have been sent to SignalR");
        Assert.That(capturedNotification!.ReaderId, Is.EqualTo(readerId));
        Assert.That(capturedNotification.ReaderName, Is.EqualTo(readerName));
        Assert.That(capturedNotification.PinLength, Is.EqualTo(pin.Length), "PinLength should match the actual PIN length");
        Assert.That(capturedNotification.CompletionReason, Is.EqualTo(completionReason), "CompletionReason should match the event's completion reason");
        Assert.That(capturedNotification.Timestamp, Is.EqualTo(timestamp));
        Assert.That(capturedNotification.Success, Is.EqualTo(result.Success));
        Assert.That(capturedNotification.Message, Is.EqualTo(result.Message));
        Assert.That(capturedNotification.Feedback, Is.EqualTo(feedback));

        // Verify SignalR was called
        _mockNotificationClient.Verify(c => c.PinEventProcessed(It.IsAny<PinEventNotification>()), Times.Once);
    }

    [Test]
    [Category("Unit")]
    public async Task OnPinProcessingCompleted_ShouldHandleDifferentPinLengthsAndCompletionReasons()
    {
        // Arrange
        var testCases = new[]
        {
            new { Pin = "1", CompletionReason = PinCompletionReason.Timeout },
            new { Pin = "12345", CompletionReason = PinCompletionReason.MaxLength },
            new { Pin = "123456789", CompletionReason = PinCompletionReason.PoundKey }
        };

        foreach (var testCase in testCases)
        {
            var pinReadEvent = new PinReadEvent
            {
                ReaderId = Guid.NewGuid(),
                ReaderName = "Test Reader",
                Pin = testCase.Pin,
                CompletionReason = testCase.CompletionReason,
                Timestamp = DateTime.UtcNow
            };

            var result = new PinReadResult
            {
                Success = true,
                Message = "PIN accepted"
            };

            var feedback = new ReaderFeedback
            {
                Type = ReaderFeedbackType.Success
            };

            var completedEvent = new PinProcessingCompletedEvent
            {
                PinRead = pinReadEvent,
                Result = result,
                Feedback = feedback,
                PersistenceSuccessful = true,
                FeedbackDeliverySuccessful = true
            };

            // Capture the PinEventNotification sent to SignalR
            PinEventNotification? capturedNotification = null;
            _mockNotificationClient
                .Setup(c => c.PinEventProcessed(It.IsAny<PinEventNotification>()))
                .Callback<PinEventNotification>(notification => capturedNotification = notification)
                .Returns(Task.CompletedTask);

            // Act
            await _service.StartAsync(CancellationToken.None);
            var subscribedHandler = GetSubscribedHandler<PinProcessingCompletedEvent>();
            await subscribedHandler(completedEvent);

            // Assert
            Assert.That(capturedNotification, Is.Not.Null, 
                $"PinEventNotification should have been sent for PIN '{testCase.Pin}' with {testCase.CompletionReason}");
            Assert.That(capturedNotification!.PinLength, Is.EqualTo(testCase.Pin.Length), 
                $"PinLength should be {testCase.Pin.Length} for PIN '{testCase.Pin}'");
            Assert.That(capturedNotification.CompletionReason, Is.EqualTo(testCase.CompletionReason), 
                $"CompletionReason should be {testCase.CompletionReason}");
        }
    }

    private Func<T, Task> GetSubscribedHandler<T>()
    {
        // This is a simplified approach - in a real test we'd capture the handler during Setup
        // For now, we'll use reflection to get the private method
        var method = typeof(UnifiedNotificationService)
            .GetMethod($"On{typeof(T).Name.Replace("Event", "")}", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method == null)
            throw new InvalidOperationException($"Handler method for {typeof(T).Name} not found");

        return async (eventData) => 
        {
            var result = method.Invoke(_service, new object[] { eventData });
            if (result is Task task)
                await task;
        };
    }
}