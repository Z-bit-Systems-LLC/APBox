using Bunit;
using ApBox.Web.Pages;
using ApBox.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ApBox.Web.Tests.Pages;

[TestFixture]
[Category("UI")]
public class IndexPageTests : ApBoxTestContext
{
    [SetUp]
    public void Setup()
    {
        ResetMocks();
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
        var pageTitle = component.Find("h1.display-4");
        Assert.That(pageTitle.TextContent, Is.EqualTo("ApBox Dashboard"));
        
        var subtitle = component.Find("p.text-muted");
        Assert.That(subtitle.TextContent, Is.EqualTo("Card Reader Management System"));
    }

    [Test]
    public void Index_DisplaysMetricCards()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert - Should have 4 metric cards
        var metricCards = component.FindAll(".metric-card");
        Assert.That(metricCards.Count, Is.EqualTo(4));

        // Check for Active Readers card
        var readersCard = component.Find(".metric-card:contains('Active Readers')");
        Assert.That(readersCard, Is.Not.Null);

        // Check for Loaded Plugins card
        var pluginsCard = component.Find(".metric-card:contains('Loaded Plugins')");
        Assert.That(pluginsCard, Is.Not.Null);

        // Check for Card Events card
        var eventsCard = component.Find(".metric-card:contains('Card Events Today')");
        Assert.That(eventsCard, Is.Not.Null);

        // Check for System Status card
        var statusCard = component.Find(".metric-card:contains('System Status')");
        Assert.That(statusCard, Is.Not.Null);
    }

    [Test]
    public void Index_DisplaysRecentEventsSection()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var eventsSection = component.Find("h5:contains('Recent Card Events')");
        Assert.That(eventsSection, Is.Not.Null);

        // Should have events table or empty message
        var hasTable = component.FindAll("table").Count > 0;
        var hasEmptyMessage = component.FindAll("p:contains('No recent card events')").Count > 0;
        Assert.That(hasTable || hasEmptyMessage, Is.True);
    }

    [Test]
    public void Index_DisplaysReaderStatusSection()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var statusSection = component.Find("h5:contains('Reader Status')");
        Assert.That(statusSection, Is.Not.Null);
    }

    [Test]
    public void Index_ShowsCorrectActiveReadersCount()
    {
        // Arrange - Mock returns 2 readers by default
        
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var readersCard = component.Find(".metric-card:contains('Active Readers')");
        var metricValue = readersCard.QuerySelector(".metric-value");
        Assert.That(metricValue?.TextContent, Is.EqualTo("2"));
    }

    [Test]
    public void Index_ShowsCorrectPluginsCount()
    {
        // Arrange - Mock returns 1 plugin by default
        
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var pluginsCard = component.Find(".metric-card:contains('Loaded Plugins')");
        var metricValue = pluginsCard.QuerySelector(".metric-value");
        Assert.That(metricValue?.TextContent, Is.EqualTo("1"));
    }

    [Test]
    public void Index_LoadsDataOnInitialization()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

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

        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var readersCard = component.Find(".metric-card:contains('Active Readers')");
        var metricValue = readersCard.QuerySelector(".metric-value");
        Assert.That(metricValue?.TextContent, Is.EqualTo("0"));

        // Should show "No readers configured" message
        var noReadersMessage = component.Find("p:contains('No readers configured')");
        Assert.That(noReadersMessage, Is.Not.Null);
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
        var pluginsCard = component.Find(".metric-card:contains('Loaded Plugins')");
        var metricValue = pluginsCard.QuerySelector(".metric-value");
        Assert.That(metricValue?.TextContent, Is.EqualTo("0"));
    }
}