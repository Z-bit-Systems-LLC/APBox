using ApBox.Core.Models;
using ApBox.Plugins;
using ApBox.Core.Services.Plugins;
using Bunit;

namespace ApBox.Web.Tests.Pages;

/// <summary>
/// Tests for the Configuration page functionality
/// </summary>
public class ConfigurationPageTests : ApBoxTestContext
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
                ReaderName = "Main Entrance"
            },
            new ReaderConfiguration
            {
                ReaderId = Guid.Parse("87654321-4321-4321-4321-cba987654321"),
                ReaderName = "Back Door"
            }
        };

        var mockPlugin = new Mock<IApBoxPlugin>();
        mockPlugin.Setup(x => x.Name).Returns("Test Plugin");
        mockPlugin.Setup(x => x.Version).Returns("1.2.3");
        mockPlugin.Setup(x => x.Description).Returns("A test plugin for configuration testing");
        
        var plugins = new List<IApBoxPlugin> { mockPlugin.Object };

        MockReaderConfigurationService.Setup(x => x.GetAllReadersAsync())
            .ReturnsAsync(readers);
        
        MockPluginLoader.Setup(x => x.LoadPluginsAsync())
            .ReturnsAsync(plugins);
    }

    [Test]
    public void ConfigurationPage_ShouldRenderCorrectly()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Configuration>();

        // Assert
        Assert.That(component.Find("h1").TextContent, Contains.Substring("Configuration"));
        Assert.That(component.Find("p").TextContent, Contains.Substring("Manage readers, feedback settings"));
    }

    [Test]
    public void ConfigurationPage_ShouldDisplayNavigationTabs()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Configuration>();

        // Assert
        var tabs = component.FindAll(".nav-link");
        Assert.That(tabs.Count, Is.EqualTo(4));
        
        var tabTexts = tabs.Select(t => t.TextContent.Trim()).ToList();
        Assert.That(tabTexts, Contains.Item("Readers"));
        Assert.That(tabTexts, Contains.Item("Feedback"));
        Assert.That(tabTexts, Contains.Item("Plugins"));
        Assert.That(tabTexts, Contains.Item("System"));
    }

    [Test]
    public void ConfigurationPage_ShouldShowReadersTabByDefault()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Configuration>();

        // Assert - check that readers tab is active by finding it with ElementId
        var readersTab = component.Find("#readers-tab");
        Assert.That(readersTab, Is.Not.Null);
        
        // Should show readers content - check if Add Reader button exists somewhere in the component
        var addReaderButton = component.Find("button:contains('Add Reader')");
        Assert.That(addReaderButton, Is.Not.Null);
    }

    [Test]
    public void ConfigurationPage_ShouldCallRequiredServicesOnInitialization()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Configuration>();

        // Assert - ReaderConfigurationService is called by ReadersConfiguration and SystemConfiguration components
        // PluginLoader is only called when switching to plugins tab
        MockReaderConfigurationService.Verify(x => x.GetAllReadersAsync(), Times.Exactly(2));
        
        // Switch to plugins tab to trigger plugin loading
        var pluginsTab = component.Find("#plugins-tab");
        pluginsTab.Click();
        
        // Now verify plugin loader was called by multiple components (PluginsConfiguration, SystemConfiguration, and ReadersConfiguration)
        MockPluginLoader.Verify(x => x.LoadPluginsAsync(), Times.Exactly(3));
    }

    [Test]
    public void ConfigurationPage_ShouldHandleTabSwitching()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Configuration>();
        
        // Switch to feedback tab
        var feedbackTab = component.Find("#feedback-tab");
        feedbackTab.Click();

        // Assert - Check that feedback configuration is loaded
        var feedbackTitle = component.Find("#feedback-title");
        Assert.That(feedbackTitle, Is.Not.Null);
        Assert.That(feedbackTitle.TextContent, Is.EqualTo("Default Feedback Configuration"));
        
        // Switch to plugins tab
        var pluginsTab = component.Find("#plugins-tab");
        pluginsTab.Click();

        // Assert
        var pluginsContent = component.Find("h3:contains('Loaded Plugins')");
        Assert.That(pluginsContent, Is.Not.Null);
    }

    [Test]
    public void ConfigurationPage_ShouldHaveResponsiveLayout()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.Configuration>();

        // Assert
        var responsiveColumns = component.FindAll(".col-md-6, .col-lg-4, .col-md-8, .col-md-4");
        Assert.That(responsiveColumns.Count, Is.GreaterThan(0));
        
        // Should use Bootstrap grid classes
        Assert.That(component.Markup, Contains.Substring("container-fluid"));
        Assert.That(component.Markup, Contains.Substring("row"));
    }
}