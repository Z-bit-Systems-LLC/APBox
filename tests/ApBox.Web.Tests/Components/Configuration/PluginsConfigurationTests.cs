using Bunit;

namespace ApBox.Web.Tests.Components.Configuration;

/// <summary>
/// Tests for the PluginsConfiguration component
/// </summary>
[TestFixture]
[Category("UI")]
public class PluginsConfigurationTests : ApBoxTestContext
{
    [SetUp]
    public void SetUp()
    {
        ResetMocks();
        SetupDefaultMocks();
    }

    [Test]
    public void PluginsConfiguration_RendersCorrectly()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Components.Configuration.PluginsConfiguration>();

        // Assert
        Assert.That(component, Is.Not.Null);
        var title = component.Find("#plugins-title");
        Assert.That(title.TextContent, Is.EqualTo("Loaded Plugins"));
    }

    [Test]
    public void PluginsConfiguration_ShowsPluginsAfterLoading()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Components.Configuration.PluginsConfiguration>();

        // Assert - After initialization, plugins should be loaded
        // The default mocks provide one test plugin
        Assert.That(component.Markup, Does.Contain("Test Plugin"));
    }

    [Test]
    public void PluginsConfiguration_ShowsRefreshButton()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Components.Configuration.PluginsConfiguration>();

        // Assert
        var refreshButton = component.Find("#refresh-plugins-btn");
        Assert.That(refreshButton, Is.Not.Null);
        Assert.That(refreshButton.TextContent.Trim(), Does.Contain("Refresh Plugins"));
    }
}