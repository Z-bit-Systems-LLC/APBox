using ApBox.Core.Models;
using ApBox.Web.Helpers;
using Blazorise;

namespace ApBox.Web.Tests.Helpers;

[TestFixture]
public class ReaderStatusHelperTests
{
    [Test]
    public void GetReaderStatusDisplay_WithDisabledReader_ReturnsSecondaryStatus()
    {
        // Arrange
        var reader = new ReaderConfiguration
        {
            ReaderId = Guid.NewGuid(),
            IsEnabled = false,
            SecurityMode = OsdpSecurityMode.Secure
        };
        var statuses = new Dictionary<Guid, bool> { [reader.ReaderId] = true };

        // Act
        var (statusText, statusColor, statusIcon) = ReaderStatusHelper.GetReaderStatusDisplay(reader, statuses);

        // Assert
        Assert.That(statusText, Is.EqualTo("Disabled"));
        Assert.That(statusColor, Is.EqualTo(Color.Secondary));
        Assert.That(statusIcon, Is.Null);
    }

    [Test]
    public void GetReaderStatusDisplay_WithOfflineReader_ReturnsDangerStatus()
    {
        // Arrange
        var reader = new ReaderConfiguration
        {
            ReaderId = Guid.NewGuid(),
            IsEnabled = true,
            SecurityMode = OsdpSecurityMode.Secure
        };
        var statuses = new Dictionary<Guid, bool> { [reader.ReaderId] = false };

        // Act
        var (statusText, statusColor, statusIcon) = ReaderStatusHelper.GetReaderStatusDisplay(reader, statuses);

        // Assert
        Assert.That(statusText, Is.EqualTo("Offline"));
        Assert.That(statusColor, Is.EqualTo(Color.Danger));
        Assert.That(statusIcon, Is.Null);
    }

    [Test]
    public void GetReaderStatusDisplay_WithSecureChannelOnline_ReturnsGreenStatus()
    {
        // Arrange
        var reader = new ReaderConfiguration
        {
            ReaderId = Guid.NewGuid(),
            IsEnabled = true,
            SecurityMode = OsdpSecurityMode.Secure
        };
        var statuses = new Dictionary<Guid, bool> { [reader.ReaderId] = true };

        // Act
        var (statusText, statusColor, statusIcon) = ReaderStatusHelper.GetReaderStatusDisplay(reader, statuses);

        // Assert
        Assert.That(statusText, Is.EqualTo("Online"));
        Assert.That(statusColor, Is.EqualTo(Color.Success));
        Assert.That(statusIcon, Is.EqualTo(IconName.Lock));
    }

    [Test]
    public void GetReaderStatusDisplay_WithInstallModeOnline_ReturnsOrangeStatus()
    {
        // Arrange - This represents the bug scenario
        var reader = new ReaderConfiguration
        {
            ReaderId = Guid.NewGuid(),
            IsEnabled = true,
            SecurityMode = OsdpSecurityMode.Install  // Reader configured for secure channel installation
        };
        var statuses = new Dictionary<Guid, bool> { [reader.ReaderId] = true };

        // Act
        var (statusText, statusColor, statusIcon) = ReaderStatusHelper.GetReaderStatusDisplay(reader, statuses);

        // Assert - This is the bug: Install mode shows Orange, but should become Green after key installation
        Assert.That(statusText, Is.EqualTo("Online"));
        Assert.That(statusColor, Is.EqualTo(Color.Warning)); // Orange - this is the bug!
        Assert.That(statusIcon, Is.EqualTo(IconName.Wrench));
    }

    [Test]
    public void GetReaderStatusDisplay_WithClearTextOnline_ReturnsOrangeStatus()
    {
        // Arrange
        var reader = new ReaderConfiguration
        {
            ReaderId = Guid.NewGuid(),
            IsEnabled = true,
            SecurityMode = OsdpSecurityMode.ClearText
        };
        var statuses = new Dictionary<Guid, bool> { [reader.ReaderId] = true };

        // Act
        var (statusText, statusColor, statusIcon) = ReaderStatusHelper.GetReaderStatusDisplay(reader, statuses);

        // Assert
        Assert.That(statusText, Is.EqualTo("Online"));
        Assert.That(statusColor, Is.EqualTo(Color.Warning));
        Assert.That(statusIcon, Is.EqualTo(IconName.Unlock));
    }

    [Test]
    public void Issue5_RegressionTest_SecureChannelTransitionShowsCorrectStatus()
    {
        // REGRESSION TEST for GitHub Issue #5:
        // "Last PD added shows orange status not green status"
        // 
        // This test ensures that after the fix, PDs that have successfully
        // transitioned from Install to Secure mode show GREEN status.

        // Arrange: Simulate multiple PDs after successful secure channel installation
        var pd1 = new ReaderConfiguration
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "PD1",
            IsEnabled = true,
            SecurityMode = OsdpSecurityMode.Secure  // Successfully transitioned to Secure
        };

        var pd2 = new ReaderConfiguration
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "PD2",
            IsEnabled = true,
            SecurityMode = OsdpSecurityMode.Secure  // Successfully transitioned to Secure
        };

        var pd3LastAdded = new ReaderConfiguration
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "PD3 (Last Added)",
            IsEnabled = true,
            SecurityMode = OsdpSecurityMode.Secure  // FIXED: Now properly in Secure mode
        };

        var statuses = new Dictionary<Guid, bool>
        {
            [pd1.ReaderId] = true,
            [pd2.ReaderId] = true,
            [pd3LastAdded.ReaderId] = true  // All are online in secure channel
        };

        // Act: Get status display for each PD
        var (status1Text, status1Color, status1Icon) = ReaderStatusHelper.GetReaderStatusDisplay(pd1, statuses);
        var (status2Text, status2Color, status2Icon) = ReaderStatusHelper.GetReaderStatusDisplay(pd2, statuses);
        var (status3Text, status3Color, status3Icon) = ReaderStatusHelper.GetReaderStatusDisplay(pd3LastAdded, statuses);

        // Assert: ALL PDs should show Green (Secure) - including the last added one
        Assert.That(status1Color, Is.EqualTo(Color.Success), "PD1 should show Green (Secure)");
        Assert.That(status1Icon, Is.EqualTo(IconName.Lock), "PD1 should show Lock icon");

        Assert.That(status2Color, Is.EqualTo(Color.Success), "PD2 should show Green (Secure)");
        Assert.That(status2Icon, Is.EqualTo(IconName.Lock), "PD2 should show Lock icon");

        // REGRESSION TEST: The last added PD should now show Green (Secure) after the fix
        Assert.That(status3Color, Is.EqualTo(Color.Success), 
            "FIXED: Last added PD should show Green (Secure mode) after security mode transition");
        Assert.That(status3Icon, Is.EqualTo(IconName.Lock), 
            "FIXED: Last added PD should show Lock icon after security mode transition");
    }

    [Test]
    public void Issue5_BeforeFix_InstallModeShowsOrangeStatus()
    {
        // This test documents the original behavior that caused Issue #5
        // PDs stuck in Install mode should show Orange status

        // Arrange: PD still in Install mode (bug scenario)
        var pdInInstallMode = new ReaderConfiguration
        {
            ReaderId = Guid.NewGuid(),
            ReaderName = "PD in Install Mode",
            IsEnabled = true,
            SecurityMode = OsdpSecurityMode.Install  // Still in Install mode
        };

        var statuses = new Dictionary<Guid, bool>
        {
            [pdInInstallMode.ReaderId] = true  // Online but still in Install mode
        };

        // Act
        var (statusText, statusColor, statusIcon) = ReaderStatusHelper.GetReaderStatusDisplay(pdInInstallMode, statuses);

        // Assert: Install mode should show Orange (this behavior is correct)
        Assert.That(statusText, Is.EqualTo("Online"));
        Assert.That(statusColor, Is.EqualTo(Color.Warning), 
            "Install mode should show Orange (Warning) status");
        Assert.That(statusIcon, Is.EqualTo(IconName.Wrench), 
            "Install mode should show Wrench icon");
    }
}