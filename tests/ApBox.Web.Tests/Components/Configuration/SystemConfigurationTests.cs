using Bunit;

namespace ApBox.Web.Tests.Components.Configuration;

/// <summary>
/// Tests for the SystemConfiguration component
/// </summary>
[TestFixture]
[Category("UI")]
public class SystemConfigurationTests : ApBoxTestContext
{
    [SetUp]
    public void SetUp()
    {
        ResetMocks();
        SetupDefaultMocks();
    }

    [Test]
    public void SystemConfiguration_RendersCorrectly()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Components.Configuration.SystemConfiguration>();

        // Assert
        Assert.That(component, Is.Not.Null);
        var title = component.Find("#system-title");
        Assert.That(title.TextContent, Is.EqualTo("System Information"));
    }

    [Test]
    public void SystemConfiguration_ShowsSystemInfo()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Components.Configuration.SystemConfiguration>();

        // Assert
        var readerCount = component.Find("#reader-count");
        Assert.That(readerCount, Is.Not.Null);
        
        var pluginCount = component.Find("#plugin-count");
        Assert.That(pluginCount, Is.Not.Null);
    }

    [Test]
    public void SystemConfiguration_ShowsActionButtons()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Components.Configuration.SystemConfiguration>();

        // Assert
        var refreshButton = component.Find("#refresh-system-btn");
        Assert.That(refreshButton, Is.Not.Null);
        Assert.That(refreshButton.TextContent.Trim(), Does.Contain("Refresh System Info"));

        var exportButton = component.Find("#export-config-btn");
        Assert.That(exportButton, Is.Not.Null);
        Assert.That(exportButton.TextContent.Trim(), Does.Contain("Export Config"));

        var importButton = component.Find("#import-config-btn");
        Assert.That(importButton, Is.Not.Null);
        Assert.That(importButton.TextContent.Trim(), Does.Contain("Import Config"));

        var restartButton = component.Find("#restart-system-btn");
        Assert.That(restartButton, Is.Not.Null);
        Assert.That(restartButton.TextContent.Trim(), Does.Contain("Restart System"));
    }

    [Test]
    public void SystemConfiguration_ShowsLogSection()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Components.Configuration.SystemConfiguration>();

        // Assert
        var refreshLogsButton = component.Find("#refresh-logs-btn");
        Assert.That(refreshLogsButton, Is.Not.Null);
        Assert.That(refreshLogsButton.TextContent.Trim(), Does.Contain("Refresh"));

        var downloadLogsButton = component.Find("#download-logs-btn");
        Assert.That(downloadLogsButton, Is.Not.Null);
        Assert.That(downloadLogsButton.TextContent.Trim(), Does.Contain("Export"));
    }
}