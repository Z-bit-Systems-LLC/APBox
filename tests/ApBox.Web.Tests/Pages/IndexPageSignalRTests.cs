using ApBox.Core.Data.Models;
using ApBox.Core.Models;
using ApBox.Plugins;
using Bunit;


namespace ApBox.Web.Tests.Pages;

/// <summary>
/// Tests for SignalR functionality and real-time updates on the Index/Dashboard page
/// </summary>
public class IndexPageSignalRTests : ApBoxTestContext
{
    [SetUp]
    public void SetUp()
    {
        ResetMocks();
        
        // Setup mock data
        var readers = new List<ReaderConfiguration>
        {
            new ReaderConfiguration
            {
                ReaderId = Guid.Parse("12345678-1234-1234-1234-123456789abc"),
                ReaderName = "Test Reader 1"
            },
            new ReaderConfiguration
            {
                ReaderId = Guid.Parse("87654321-4321-4321-4321-cba987654321"),
                ReaderName = "Test Reader 2"
            }
        };

        var plugins = new List<IApBoxPlugin>();

        MockReaderService.Setup(x => x.GetReadersAsync())
            .ReturnsAsync(readers);
        
        MockReaderService.Setup(x => x.GetAllReaderStatusesAsync())
            .ReturnsAsync(new Dictionary<Guid, bool>
            {
                { Guid.Parse("12345678-1234-1234-1234-123456789abc"), true },
                { Guid.Parse("87654321-4321-4321-4321-cba987654321"), true }
            });
        
        MockPluginLoader.Setup(x => x.LoadPluginsAsync())
            .ReturnsAsync(plugins);

        // Setup mock card events
        var cardEvents = new List<CardEventEntity>
        {
            new CardEventEntity
            {
                Id = 1,
                ReaderId = "12345678-1234-1234-1234-123456789abc",
                CardNumber = "123456789",
                BitLength = 26,
                ReaderName = "Test Reader 1",
                Success = true,
                Message = "Success",
                ProcessedByPlugin = "Test Plugin",
                Timestamp = DateTime.Now.AddMinutes(-5)
            },
            new CardEventEntity
            {
                Id = 2,
                ReaderId = "87654321-4321-4321-4321-cba987654321",
                CardNumber = "987654321",
                BitLength = 26,
                ReaderName = "Test Reader 2",
                Success = true,
                Message = "Success",
                ProcessedByPlugin = "Test Plugin",
                Timestamp = DateTime.Now.AddMinutes(-10)
            }
        };

        MockCardEventRepository.Setup(x => x.GetRecentAsync(It.IsAny<int>()))
            .ReturnsAsync(cardEvents);

        MockCardEventRepository.Setup(x => x.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>()))
            .ReturnsAsync(cardEvents.Take(5));
    }

    [Test]
    public void IndexPage_ShouldDisplayRecentCardEventsTable()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var cardEventsSection = component.Find("#recent-events-section");
        Assert.That(cardEventsSection, Is.Not.Null);
        
        var table = component.Find("#recent-events-table");
        Assert.That(table, Is.Not.Null);
        
        // Check table headers
        var headers = component.FindAll("thead th");
        Assert.That(headers.Count, Is.EqualTo(4));
        Assert.That(headers[0].TextContent, Is.EqualTo("Time"));
        Assert.That(headers[1].TextContent, Is.EqualTo("Reader"));
        Assert.That(headers[2].TextContent, Is.EqualTo("Card Number"));
        Assert.That(headers[3].TextContent, Is.EqualTo("Status"));
    }

    [Test]
    public void IndexPage_ShouldDisplaySampleCardEvents()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var tableRows = component.FindAll("tbody tr");
        Assert.That(tableRows.Count, Is.GreaterThan(0), "Should display sample card events");
        
        // Check that events are displayed with proper structure
        var firstRow = tableRows[0];
        var cells = firstRow.QuerySelectorAll("td");
        Assert.That(cells.Length, Is.EqualTo(4));
        
        // Time column should have time format
        var timeCell = cells[0];
        Assert.That(timeCell.TextContent, Does.Match(@"\d{2}:\d{2}:\d{2}"));
        
        // Reader name should be present
        var readerCell = cells[1];
        Assert.That(readerCell.TextContent, Is.Not.Empty);
        
        // Card number should be present
        var cardCell = cells[2];
        Assert.That(cardCell.TextContent, Is.Not.Empty);
        
        // Status should be "Processed"
        var statusCell = cells[3];
        var badge = statusCell.QuerySelector(".badge");
        Assert.That(badge, Is.Not.Null);
        Assert.That(badge.TextContent, Is.EqualTo("Processed"));
    }

    [Test]
    public void IndexPage_ShouldLimitEventsToTen()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var tableRows = component.FindAll("tbody tr");
        Assert.That(tableRows.Count, Is.LessThanOrEqualTo(10), "Should limit display to 10 events maximum");
    }

    [Test]
    public void IndexPage_ShouldDisplayMetricsCards()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert - Check each metric card by ElementId
        var activeReadersCard = component.Find("#active-readers-card");
        Assert.That(activeReadersCard, Is.Not.Null);
        
        var loadedPluginsCard = component.Find("#loaded-plugins-card");
        Assert.That(loadedPluginsCard, Is.Not.Null);
        
        var cardEventsCard = component.Find("#card-events-card");
        Assert.That(cardEventsCard, Is.Not.Null);
        
        var systemStatusCard = component.Find("#system-status-card");
        Assert.That(systemStatusCard, Is.Not.Null);
    }

    [Test]
    public void IndexPage_ShouldDisplayReaderStatusPanel()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var readerStatusSection = component.Find("#reader-status-section");
        Assert.That(readerStatusSection, Is.Not.Null);
        
        // Check for "Online" status badges
        var statusBadge = component.Find("#reader-status-badge");
        Assert.That(statusBadge, Is.Not.Null);
        Assert.That(statusBadge.TextContent, Is.EqualTo("Online"));
    }

    [Test]
    public void IndexPage_ShouldHandleNoReadersScenario()
    {
        // Arrange - Setup empty readers list
        MockReaderService.Setup(x => x.GetReadersAsync())
            .ReturnsAsync(new List<ReaderConfiguration>());

        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var noReadersMessage = component.Find("#no-readers-message");
        Assert.That(noReadersMessage, Is.Not.Null);
    }

    [Test]
    public void IndexPage_ShouldCallRequiredServicesOnInitialization()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        MockReaderService.Verify(x => x.GetReadersAsync(), Times.Once);
        MockPluginLoader.Verify(x => x.LoadPluginsAsync(), Times.Once);
    }

    [Test]
    public void IndexPage_ShouldDisplayCorrectActiveReadersCount()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var activeReadersValue = component.Find("#active-readers-value");
        Assert.That(activeReadersValue, Is.Not.Null);
        Assert.That(activeReadersValue.TextContent, Is.EqualTo("2")); // Based on our mock setup
    }

    [Test]
    public void IndexPage_ShouldDisplayCorrectLoadedPluginsCount()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var loadedPluginsValue = component.Find("#loaded-plugins-value");
        Assert.That(loadedPluginsValue, Is.Not.Null);
        Assert.That(loadedPluginsValue.TextContent, Is.EqualTo("0")); // Based on our mock setup (empty plugins list)
    }

    [Test]
    public void IndexPage_ShouldShowSystemStatusAsOnline()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var systemStatusValue = component.Find("#system-status-value");
        Assert.That(systemStatusValue, Is.Not.Null);
        Assert.That(systemStatusValue.TextContent, Is.EqualTo("Online"));
    }

    [Test]
    public void IndexPage_ShouldDisplayCardEventsTodayCount()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var cardEventsValue = component.Find("#card-events-value");
        Assert.That(cardEventsValue, Is.Not.Null);
        
        // Should be a number (from sample events)
        var value = cardEventsValue.TextContent;
        Assert.That(int.TryParse(value, out _), Is.True, "Card events today should be a number");
    }
}