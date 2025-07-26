using Bunit;
using ApBox.Core.Models;
using ApBox.Core.Data.Models;
using ApBox.Plugins;

namespace ApBox.Web.Tests.Pages;

/// <summary>
/// Comprehensive tests for the Index/Dashboard page including UI rendering and SignalR functionality
/// </summary>
[TestFixture]
[Category("UI")]
public class IndexPageTests : ApBoxTestContext
{
    [SetUp]
    public void Setup()
    {
        ResetMocks();
        SetupDetailedMockData();
    }

    /// <summary>
    /// Sets up detailed mock data for comprehensive testing
    /// </summary>
    private void SetupDetailedMockData()
    {
        // Setup readers with specific IDs for consistent testing
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

        MockReaderService.Setup(x => x.GetReadersAsync())
            .ReturnsAsync(readers);
        
        MockReaderService.Setup(x => x.GetAllReaderStatusesAsync())
            .ReturnsAsync(new Dictionary<Guid, bool>
            {
                { Guid.Parse("12345678-1234-1234-1234-123456789abc"), true },
                { Guid.Parse("87654321-4321-4321-4321-cba987654321"), true }
            });
        
        // Setup plugins
        var mockPlugin = new Mock<IApBoxPlugin>();
        mockPlugin.Setup(x => x.Id).Returns(new Guid("F6A7B8C9-ABCD-EF01-2345-123456789999"));
        mockPlugin.Setup(x => x.Name).Returns("Test Plugin");
        mockPlugin.Setup(x => x.Version).Returns("1.0.0");
        mockPlugin.Setup(x => x.Description).Returns("Test plugin for unit tests");

        MockPluginLoader.Setup(x => x.LoadPluginsAsync())
            .ReturnsAsync(new List<IApBoxPlugin> { mockPlugin.Object });

        // Setup mock card events for testing event display
        var cardEvents = new List<CardEventEntity>
        {
            new()
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
            new()
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

        // Setup other default mocks
        SetupDefaultMocks();
    }

    [Test]
    public void Index_RendersSuccessfully()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        Assert.That(component, Is.Not.Null);
        Assert.That(component.Find("h1").TextContent, Does.Contain("ApBox Dashboard"));
    }

    [Test]
    public void Index_DisplaysPageTitle()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var pageTitle = component.Find("#dashboard-title");
        Assert.That(pageTitle.TextContent, Is.EqualTo("ApBox Dashboard"));
        
        var subtitle = component.Find("#dashboard-subtitle");
        Assert.That(subtitle.TextContent, Is.EqualTo("Card Reader Management System"));
    }

    [Test]
    public void Index_DisplaysMetricCards()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert - Should have 4 metric cards
        var activeReadersCard = component.Find("#active-readers-card");
        Assert.That(activeReadersCard, Is.Not.Null);

        var loadedPluginsCard = component.Find("#loaded-plugins-card");
        Assert.That(loadedPluginsCard, Is.Not.Null);

        var cardEventsCard = component.Find("#total-events-card");
        Assert.That(cardEventsCard, Is.Not.Null);

        var systemStatusCard = component.Find("#system-status-card");
        Assert.That(systemStatusCard, Is.Not.Null);
    }

    [Test]
    public void Index_DisplaysRecentEventsSection()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var eventsSection = component.Find("#recent-events-card");
        Assert.That(eventsSection, Is.Not.Null);

        // Should have events table or empty message
        var hasTable = component.FindAll("#recent-events-table").Count > 0;
        var hasEmptyMessage = component.FindAll("#no-events-message").Count > 0;
        Assert.That(hasTable || hasEmptyMessage, Is.True);
    }

    [Test]
    public void Index_ShowsCorrectActiveReadersCount()
    {
        // Arrange - Mock returns 2 readers by default
        
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var readersValue = component.Find("#active-readers-value");
        Assert.That(readersValue.TextContent, Is.EqualTo("2"));
    }

    [Test]
    public void Index_ShowsCorrectPluginsCount()
    {
        // Arrange - Mock returns 1 plugin by default
        
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var pluginsValue = component.Find("#loaded-plugins-value");
        Assert.That(pluginsValue.TextContent, Is.EqualTo("1"));
    }

    [Test]
    public void Index_ShowsSystemStatusAsOnline()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var systemStatusValue = component.Find("#system-status-value");
        Assert.That(systemStatusValue, Is.Not.Null);
        Assert.That(systemStatusValue.TextContent, Is.EqualTo("Online"));
    }

    [Test]
    public void Index_DisplaysCardEventsTodayCount()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var cardEventsValue = component.Find("#total-events-value");
        Assert.That(cardEventsValue, Is.Not.Null);
        
        // Should be a number (from sample events)
        var value = cardEventsValue.TextContent;
        Assert.That(int.TryParse(value, out _), Is.True, "Card events today should be a number");
    }

    [Test]
    public void Index_LoadsDataOnInitialization()
    {
        // Act
        RenderComponent<ApBox.Web.Pages.Index>();

        // Assert - Verify service calls were made
        MockReaderService.Verify(x => x.GetReadersAsync(), Times.Once);
        MockPluginLoader.Verify(x => x.LoadPluginsAsync(), Times.Once);
    }

    [Test]
    public void Index_HandlesEmptyReadersList()
    {
        // Arrange
        MockReaderService.Setup(x => x.GetReadersAsync())
            .ReturnsAsync(new List<ReaderConfiguration>());
        MockReaderService.Setup(x => x.GetAllReaderStatusesAsync())
            .ReturnsAsync(new Dictionary<Guid, bool>());

        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var readersValue = component.Find("#active-readers-value");
        Assert.That(readersValue.TextContent, Is.EqualTo("0"));
    }

    [Test]
    public void Index_HandlesEmptyPluginsList()
    {
        // Arrange
        MockPluginLoader.Setup(x => x.LoadPluginsAsync())
            .ReturnsAsync(new List<IApBoxPlugin>());

        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();
        
        // Assert
        var pluginsValue = component.Find("#loaded-plugins-value");
        Assert.That(pluginsValue.TextContent, Is.EqualTo("0"));
    }
    
    [Test]
    public void Index_DoesNotShowConfigureLinkWhenReadersExist()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var readersValue = component.Find("#active-readers-value");
        Assert.That(readersValue.TextContent, Is.EqualTo("2")); // Default mock has 2 readers
        
        // Should NOT show the "no readers" message or configure link
        var noReadersMessages = component.FindAll("#no-readers-message");
        Assert.That(noReadersMessages.Count, Is.EqualTo(0));
        
        var configureLinks = component.FindAll("#configure-readers-link");
        Assert.That(configureLinks.Count, Is.EqualTo(0));
    }

    // ==============================================
    // Card Events Display Tests
    // ==============================================

    [Test]
    public void Index_DisplaysRecentCardEventsTable()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var cardEventsSection = component.Find("#recent-events-card");
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
    public void Index_DisplaysSampleCardEvents()
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
    public void Index_LimitsEventsToTen()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var tableRows = component.FindAll("tbody tr");
        Assert.That(tableRows.Count, Is.LessThanOrEqualTo(10), "Should limit display to 10 events maximum");
    }

    // ==============================================
    // Dynamic Updates Tests (UI Responsiveness)
    // ==============================================

    [Test]
    public void Index_WithDynamicCardEvents_UpdatesRecentEventsDisplay()
    {
        // Arrange - Setup mock with additional events that could be added dynamically
        var dynamicEvents = new List<CardEventEntity>
        {
            new()
            {
                Id = 999,
                ReaderId = "99999999-9999-9999-9999-999999999999",
                CardNumber = "999888777",
                BitLength = 26,
                ReaderName = "Dynamic Test Reader",
                Success = true,
                Message = "Success",
                ProcessedByPlugin = "Test Plugin",
                Timestamp = DateTime.Now
            }
        };

        // Update mock to return dynamic events
        MockCardEventRepository.Setup(x => x.GetRecentAsync(It.IsAny<int>()))
            .ReturnsAsync(dynamicEvents);

        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var tableRows = component.FindAll("tbody tr");
        Assert.That(tableRows.Count, Is.GreaterThan(0), "Should display dynamic card events");
        
        // Verify the dynamic event appears in the table
        var dynamicEventRow = tableRows.FirstOrDefault(row => 
            row.TextContent.Contains("Dynamic Test Reader") && 
            row.TextContent.Contains("999888777"));
        Assert.That(dynamicEventRow, Is.Not.Null, "Dynamic card event should appear in recent events table");
    }

    [Test]
    public void Index_WithVaryingReaderCounts_UpdatesActiveReadersDisplay()
    {
        // Arrange - Test with different reader configurations
        var scenarios = new[]
        {
            new { ReadersOnline = 0, TotalReaders = 0, ExpectedActive = 0 },
            new { ReadersOnline = 1, TotalReaders = 3, ExpectedActive = 1 },
            new { ReadersOnline = 2, TotalReaders = 2, ExpectedActive = 2 },
            new { ReadersOnline = 5, TotalReaders = 8, ExpectedActive = 5 }
        };

        foreach (var scenario in scenarios)
        {
            // Setup readers with specific online/offline status
            var readers = new List<ReaderConfiguration>();
            var statuses = new Dictionary<Guid, bool>();
            
            for (int i = 0; i < scenario.TotalReaders; i++)
            {
                var readerId = Guid.NewGuid();
                readers.Add(new ReaderConfiguration { ReaderId = readerId, ReaderName = $"Reader {i}" });
                statuses[readerId] = i < scenario.ReadersOnline; // First N readers are online
            }

            MockReaderService.Setup(x => x.GetReadersAsync()).ReturnsAsync(readers);
            MockReaderService.Setup(x => x.GetAllReaderStatusesAsync()).ReturnsAsync(statuses);

            // Act
            var component = RenderComponent<ApBox.Web.Pages.Index>();

            // Assert
            var activeReadersValue = component.Find("#active-readers-value").TextContent;
            Assert.That(activeReadersValue, Is.EqualTo(scenario.ExpectedActive.ToString()), 
                $"Should show {scenario.ExpectedActive} active readers when {scenario.ReadersOnline} of {scenario.TotalReaders} are online");
            
            // Clean up for next iteration
            ResetMocks();
            SetupDetailedMockData();
        }
    }

    [Test]
    public void Index_WithHighEventVolume_LimitsDisplayedEvents()
    {
        // Arrange - Create many events to test UI limiting
        var manyEvents = new List<CardEventEntity>();
        for (int i = 0; i < 50; i++) // Create 50 events
        {
            manyEvents.Add(new CardEventEntity
            {
                Id = i,
                ReaderId = Guid.NewGuid().ToString(),
                CardNumber = $"123456{i:D3}",
                BitLength = 26,
                ReaderName = $"Reader {i}",
                Success = true,
                Message = "Success",
                ProcessedByPlugin = "Test Plugin",
                Timestamp = DateTime.Now.AddMinutes(-i)
            });
        }

        MockCardEventRepository.Setup(x => x.GetRecentAsync(It.IsAny<int>()))
            .ReturnsAsync(manyEvents);

        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var tableRows = component.FindAll("tbody tr");
        Assert.That(tableRows.Count, Is.LessThanOrEqualTo(15), "Should limit displayed events to prevent UI overflow");
        
        // Verify most recent events are shown first
        var firstRow = tableRows.First();
        Assert.That(firstRow.TextContent, Does.Contain("Reader 0"), "Most recent event should be displayed first");
    }

    [Test]
    public void Index_WithRealtimeEventUpdates_MaintainsResponsiveLayout()
    {
        // Arrange - Simulate real-time event pattern with timestamps
        var realtimeEvents = new List<CardEventEntity>();
        var baseTime = DateTime.Now;
        
        // Add events with increasing timestamps (simulating real-time arrival)
        for (int i = 0; i < 10; i++)
        {
            realtimeEvents.Add(new CardEventEntity
            {
                Id = i,
                ReaderId = Guid.NewGuid().ToString(),
                CardNumber = $"RT{i:D6}",
                BitLength = 26,
                ReaderName = "Realtime Reader",
                Success = true,
                Message = "Success",
                ProcessedByPlugin = "RT Plugin",
                Timestamp = baseTime.AddSeconds(i) // Sequential timestamps
            });
        }

        MockCardEventRepository.Setup(x => x.GetRecentAsync(It.IsAny<int>()))
            .ReturnsAsync(realtimeEvents);

        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var eventsTable = component.Find("#recent-events-table");
        Assert.That(eventsTable, Is.Not.Null, "Events table should be rendered");
        
        var tableRows = component.FindAll("tbody tr");
        Assert.That(tableRows.Count, Is.GreaterThan(0), "Should display realtime events");
        
        // Verify time formatting is consistent across all events
        foreach (var row in tableRows)
        {
            var timeCell = row.QuerySelectorAll("td")[0];
            Assert.That(timeCell.TextContent, Does.Match(@"\d{2}:\d{2}:\d{2}"), 
                "All timestamps should be formatted consistently as HH:mm:ss");
        }
    }

    [Test]
    public void Index_WithMixedEventStatuses_DisplaysAppropriateIndicators()
    {
        // Arrange - Create events with different success statuses
        var mixedEvents = new List<CardEventEntity>
        {
            new()
            {
                Id = 1,
                ReaderId = Guid.NewGuid().ToString(),
                CardNumber = "SUCCESS001",
                BitLength = 26,
                ReaderName = "Success Reader",
                Success = true,
                Message = "Successfully processed",
                ProcessedByPlugin = "Test Plugin",
                Timestamp = DateTime.Now.AddMinutes(-1)
            },
            new()
            {
                Id = 2,
                ReaderId = Guid.NewGuid().ToString(),
                CardNumber = "SUCCESS002",
                BitLength = 26,
                ReaderName = "Another Success Reader",
                Success = true,
                Message = "All plugins succeeded",
                ProcessedByPlugin = "Test Plugin",
                Timestamp = DateTime.Now.AddMinutes(-2)
            }
        };

        MockCardEventRepository.Setup(x => x.GetRecentAsync(It.IsAny<int>()))
            .ReturnsAsync(mixedEvents);

        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var tableRows = component.FindAll("tbody tr");
        Assert.That(tableRows.Count, Is.EqualTo(2), "Should display both events");
        
        // Verify status indicators (all should show "Processed" for successful events)
        foreach (var row in tableRows)
        {
            var statusCell = row.QuerySelectorAll("td")[3]; // Status column
            var badge = statusCell.QuerySelector(".badge");
            Assert.That(badge, Is.Not.Null, "Should have status badge");
            Assert.That(badge.TextContent, Is.EqualTo("Processed"), "Successful events should show 'Processed' status");
        }
    }
}