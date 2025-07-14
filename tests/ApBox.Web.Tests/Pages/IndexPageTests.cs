using Bunit;
using ApBox.Web.Pages;
using ApBox.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using AngleSharp.Dom;

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

        var cardEventsCard = component.Find("#card-events-card");
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
        var eventsSection = component.Find("#recent-events-section");
        Assert.That(eventsSection, Is.Not.Null);

        // Should have events table or empty message
        var hasTable = component.FindAll("#recent-events-table").Count > 0;
        var hasEmptyMessage = component.FindAll("#no-events-message").Count > 0;
        Assert.That(hasTable || hasEmptyMessage, Is.True);
    }

    [Test]
    public void Index_DisplaysReaderStatusSection()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Index>();

        // Assert
        var statusSection = component.Find("#reader-status-section");
        Assert.That(statusSection, Is.Not.Null);
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
        var readersValue = component.Find("#active-readers-value");
        Assert.That(readersValue.TextContent, Is.EqualTo("0"));

        // Should show "No readers configured" message
        var noReadersMessage = component.Find("#no-readers-message");
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
        var pluginsValue = component.Find("#loaded-plugins-value");
        Assert.That(pluginsValue.TextContent, Is.EqualTo("0"));
    }
}