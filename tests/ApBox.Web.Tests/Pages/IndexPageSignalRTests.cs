using ApBox.Core.Services;
using ApBox.Plugins;
using ApBox.Web.Hubs;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using AngleSharp.Dom;
using Microsoft.AspNetCore.Components;

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
        
        MockPluginLoader.Setup(x => x.LoadPluginsAsync())
            .ReturnsAsync(plugins);
    }

    [Test]
    public void IndexPage_ShouldDisplayRecentCardEventsTable()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var cardEventsCard = component.Find("h5:contains('Recent Card Events')");
        Assert.That(cardEventsCard, Is.Not.Null);
        
        var table = component.Find("table");
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
        var firstRow = tableRows.First();
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

        // Assert
        var metricCards = component.FindAll(".metric-card");
        Assert.That(metricCards.Count, Is.EqualTo(4));
        
        // Check each metric card
        var activeReadersCard = component.Find(".metric-card:contains('Active Readers')");
        Assert.That(activeReadersCard, Is.Not.Null);
        
        var loadedPluginsCard = component.Find(".metric-card:contains('Loaded Plugins')");
        Assert.That(loadedPluginsCard, Is.Not.Null);
        
        var cardEventsCard = component.Find(".metric-card:contains('Card Events Today')");
        Assert.That(cardEventsCard, Is.Not.Null);
        
        var systemStatusCard = component.Find(".metric-card:contains('System Status')");
        Assert.That(systemStatusCard, Is.Not.Null);
    }

    [Test]
    public void IndexPage_ShouldDisplayReaderStatusPanel()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var readerStatusCard = component.Find("h5:contains('Reader Status')");
        Assert.That(readerStatusCard, Is.Not.Null);
        
        // Should show configured readers
        var readerEntries = component.FindAll(".d-flex:has(.badge)");
        Assert.That(readerEntries.Count, Is.GreaterThan(0));
        
        // Check for "Online" badges
        var onlineBadges = component.FindAll(".badge.bg-success:contains('Online')");
        Assert.That(onlineBadges.Count, Is.GreaterThan(0));
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
        var noReadersMessage = component.Find("p:contains('No readers configured')");
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
        var activeReadersMetric = component.Find(".metric-card:contains('Active Readers') .metric-value");
        Assert.That(activeReadersMetric, Is.Not.Null);
        Assert.That(activeReadersMetric.TextContent, Is.EqualTo("2")); // Based on our mock setup
    }

    [Test]
    public void IndexPage_ShouldDisplayCorrectLoadedPluginsCount()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var loadedPluginsMetric = component.Find(".metric-card:contains('Loaded Plugins') .metric-value");
        Assert.That(loadedPluginsMetric, Is.Not.Null);
        Assert.That(loadedPluginsMetric.TextContent, Is.EqualTo("0")); // Based on our mock setup (empty plugins list)
    }

    [Test]
    public void IndexPage_ShouldShowSystemStatusAsOnline()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var systemStatusMetric = component.Find(".metric-card:contains('System Status') .metric-value");
        Assert.That(systemStatusMetric, Is.Not.Null);
        Assert.That(systemStatusMetric.TextContent, Is.EqualTo("Online"));
    }

    [Test]
    public void IndexPage_ShouldDisplayCardEventsTodayCount()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var cardEventsTodayMetric = component.Find(".metric-card:contains('Card Events Today') .metric-value");
        Assert.That(cardEventsTodayMetric, Is.Not.Null);
        
        // Should be a number (from sample events)
        var value = cardEventsTodayMetric.TextContent;
        Assert.That(int.TryParse(value, out _), Is.True, "Card events today should be a number");
    }
}