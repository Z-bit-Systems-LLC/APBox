using Bunit;
using ApBox.Core.Models;
using ApBox.Plugins;
using ApBox.Core.Services.Plugins;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ApBox.Web.Tests.Components.Configuration;

/// <summary>
/// Tests for the FeedbackConfiguration component
/// </summary>
[TestFixture]
[Category("UI")]
public class FeedbackConfigurationTests : ApBoxTestContext
{
    [SetUp]
    public void SetUp()
    {
        ResetMocks();
        SetupDefaultMocks();
    }

    #region Component Rendering Tests

    [Test]
    public void FeedbackConfiguration_RendersCorrectly()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Components.Configuration.FeedbackConfiguration>();

        // Assert
        Assert.That(component, Is.Not.Null);
        var title = component.Find("#feedback-title");
        Assert.That(title.TextContent, Is.EqualTo("Default Feedback Configuration"));
    }

    [Test]
    public async Task FeedbackConfiguration_ShowsLoadingState()
    {
        // Arrange - Set up a delay in the service call
        MockFeedbackConfigurationService.Setup(x => x.GetDefaultConfigurationAsync())
            .Returns(async () =>
            {
                await Task.Delay(100);
                return GetDefaultFeedbackConfiguration();
            });

        // Act
        var component = RenderComponent<ApBox.Web.Components.Configuration.FeedbackConfiguration>();

        // Assert - Should show loading initially
        var loadingAlert = component.Find(".alert");
        Assert.That(loadingAlert.TextContent, Does.Contain("Loading feedback configuration"));
        
        // Wait for loading to complete
        await Task.Delay(150);
        component.Render();
        
        // Should show the configuration forms after loading
        var successCard = component.Find("[data-testid='success-feedback-card'], .card:contains('Success Feedback')");
        Assert.That(successCard, Is.Not.Null);
    }

    [Test]
    public void FeedbackConfiguration_DisplaysSuccessAndFailureCards()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Components.Configuration.FeedbackConfiguration>();

        // Assert
        var cards = component.FindAll(".card");
        Assert.That(cards.Count, Is.GreaterThanOrEqualTo(3)); // Success, Failure, Idle State

        // Check for specific card titles
        var cardTitles = component.FindAll(".card-title");
        var titleTexts = cardTitles.Select(t => t.TextContent.Trim()).ToList();
        
        Assert.That(titleTexts, Does.Contain("Success Feedback"));
        Assert.That(titleTexts, Does.Contain("Failure Feedback"));
        Assert.That(titleTexts, Does.Contain("Idle State Configuration"));
    }

    [Test]
    public void FeedbackConfiguration_DisplaysCorrectFormFields()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Components.Configuration.FeedbackConfiguration>();

        // Assert - Check for success form fields
        Assert.That(component.Find("#success-led-color"), Is.Not.Null);
        Assert.That(component.Find("#success-led-duration"), Is.Not.Null);
        Assert.That(component.Find("#success-beep-count"), Is.Not.Null);
        Assert.That(component.Find("#success-display-message"), Is.Not.Null);

        // Assert - Check for failure form fields  
        Assert.That(component.Find("#failure-led-color"), Is.Not.Null);
        Assert.That(component.Find("#failure-led-duration"), Is.Not.Null);
        Assert.That(component.Find("#failure-beep-count"), Is.Not.Null);
        Assert.That(component.Find("#failure-display-message"), Is.Not.Null);

        // Assert - Check for idle state fields
        Assert.That(component.Find("#idle-permanent-led-color"), Is.Not.Null);
        Assert.That(component.Find("#idle-heartbeat-flash-color"), Is.Not.Null);
    }

    #endregion

    #region Form Interaction Tests

    [Test]
    public void FeedbackConfiguration_LoadsDefaultValues()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Components.Configuration.FeedbackConfiguration>();

        // Assert - Check that default values are loaded
        var successLedColor = component.Find("#success-led-color");
        Assert.That(successLedColor.GetAttribute("value"), Does.Contain("Green"));

        var successDisplayMessage = component.Find("#success-display-message");
        Assert.That(successDisplayMessage.GetAttribute("value"), Is.EqualTo("ACCESS GRANTED"));

        var failureLedColor = component.Find("#failure-led-color");
        Assert.That(failureLedColor.GetAttribute("value"), Does.Contain("Red"));

        var failureDisplayMessage = component.Find("#failure-display-message");
        Assert.That(failureDisplayMessage.GetAttribute("value"), Is.EqualTo("ACCESS DENIED"));
    }

    [Test]
    public async Task FeedbackConfiguration_UpdatesPreviewWhenFormChanges()
    {
        // Arrange
        var component = RenderComponent<ApBox.Web.Components.Configuration.FeedbackConfiguration>();

        // Act - Change success LED color
        var successLedSelect = component.Find("#success-led-color");
        await successLedSelect.ChangeAsync(new ChangeEventArgs { Value = "Amber" });

        // Assert - Preview should update
        var previewBadge = component.Find("#success-led-color-badge");
        Assert.That(previewBadge.TextContent.Contains("Amber"), Is.True, "Preview should show updated LED color");
    }

    [Test]
    public async Task FeedbackConfiguration_AutoSavesOnSuccessFormChange()
    {
        // Arrange
        var component = RenderComponent<ApBox.Web.Components.Configuration.FeedbackConfiguration>();

        // Act - Change success LED color
        var successLedSelect = component.Find("#success-led-color");
        await successLedSelect.ChangeAsync(new ChangeEventArgs { Value = "Amber" });

        // Assert - Auto-save should call individual save method
        MockFeedbackConfigurationService.Verify(
            x => x.SaveSuccessFeedbackAsync(It.IsAny<ReaderFeedback>()), 
            Times.AtLeastOnce);
    }

    [Test]
    public async Task FeedbackConfiguration_AutoSavesOnFailureFormChange()
    {
        // Arrange
        var component = RenderComponent<ApBox.Web.Components.Configuration.FeedbackConfiguration>();

        // Act - Change failure beep count
        var failureBeepInput = component.Find("#failure-beep-count");
        await failureBeepInput.InputAsync(new ChangeEventArgs { Value = "5" });

        // Assert - Auto-save should call individual save method
        MockFeedbackConfigurationService.Verify(
            x => x.SaveFailureFeedbackAsync(It.IsAny<ReaderFeedback>()), 
            Times.AtLeastOnce);
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public async Task FeedbackConfiguration_HandlesLoadError()
    {
        // Arrange
        MockFeedbackConfigurationService.Setup(x => x.GetDefaultConfigurationAsync())
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var component = RenderComponent<ApBox.Web.Components.Configuration.FeedbackConfiguration>();
        await Task.Delay(50); // Wait for error to be handled

        // Assert - Component should handle error gracefully and still render
        Assert.That(component, Is.Not.Null);
        var title = component.Find("#feedback-title");
        Assert.That(title.TextContent, Is.EqualTo("Default Feedback Configuration"));
    }

    [Test]
    public async Task FeedbackConfiguration_HandlesAutoSaveError()
    {
        // Arrange
        MockFeedbackConfigurationService.Setup(x => x.SaveSuccessFeedbackAsync(It.IsAny<ReaderFeedback>()))
            .ThrowsAsync(new Exception("Auto-save failed"));

        var component = RenderComponent<ApBox.Web.Components.Configuration.FeedbackConfiguration>();
        await Task.Delay(50); // Wait for initial load

        // Act - Change a value to trigger auto-save
        var successLedSelect = component.Find("#success-led-color");
        
        // Assert - Should not throw and should handle error gracefully
        Assert.DoesNotThrowAsync(async () => await successLedSelect.ChangeAsync(new ChangeEventArgs { Value = "Amber" }));
    }

    #endregion

    #region Preview Tests

    [Test]
    public void FeedbackConfiguration_ShowsSuccessPreview()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Components.Configuration.FeedbackConfiguration>();

        // Assert - Success preview should be embedded in success card
        var successCard = component.FindAll(".card").First(c => c.TextContent.Contains("Success Feedback"));
        var previewHeadings = successCard.QuerySelectorAll("h6").Where(h => h.TextContent.Contains("Preview"));
        Assert.That(previewHeadings.Count(), Is.GreaterThan(0), "Success card should contain preview section");
    }

    [Test]
    public void FeedbackConfiguration_ShowsFailurePreview()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Components.Configuration.FeedbackConfiguration>();

        // Assert - Failure preview should be embedded in failure card
        var failureCard = component.FindAll(".card").First(c => c.TextContent.Contains("Failure Feedback"));
        var previewHeadings = failureCard.QuerySelectorAll("h6").Where(h => h.TextContent.Contains("Preview"));
        Assert.That(previewHeadings.Count(), Is.GreaterThan(0), "Failure card should contain preview section");
    }

    [Test]
    public void FeedbackConfiguration_ShowsIdleStatePreview()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Components.Configuration.FeedbackConfiguration>();

        // Assert - Idle state preview should be in idle state card
        var idleCard = component.FindAll(".card").First(c => c.TextContent.Contains("Idle State"));
        var previewHeadings = idleCard.QuerySelectorAll("h6").Where(h => h.TextContent.Contains("Preview"));
        Assert.That(previewHeadings.Count(), Is.GreaterThan(0), "Idle state card should contain preview section");
    }

    #endregion

    #region Service Integration Tests

    [Test]
    public async Task FeedbackConfiguration_CallsServiceOnInitialization()
    {
        // Act
        RenderComponent<ApBox.Web.Components.Configuration.FeedbackConfiguration>();
        await Task.Delay(50); // Allow component to initialize

        // Assert
        MockFeedbackConfigurationService.Verify(x => x.GetDefaultConfigurationAsync(), Times.Once);
    }

    [Test]
    public async Task FeedbackConfiguration_PassesCorrectDataToAutoSave()
    {
        // Arrange
        var component = RenderComponent<ApBox.Web.Components.Configuration.FeedbackConfiguration>();
        await Task.Delay(50); // Wait for initial load

        // Act - Modify success feedback
        var successDisplayMessage = component.Find("#success-display-message");
        await successDisplayMessage.InputAsync(new ChangeEventArgs { Value = "WELCOME" });

        // Wait for auto-save
        await Task.Delay(100);

        // Assert - Should auto-save success feedback with correct data
        MockFeedbackConfigurationService.Verify(
            x => x.SaveSuccessFeedbackAsync(
                It.Is<ReaderFeedback>(feedback =>
                    feedback.DisplayMessage == "WELCOME" &&
                    feedback.Type == ReaderFeedbackType.Success)),
            Times.AtLeastOnce);

        // Act - Modify failure feedback
        var failureBeepCount = component.Find("#failure-beep-count");
        await failureBeepCount.InputAsync(new ChangeEventArgs { Value = "5" });

        // Wait for auto-save
        await Task.Delay(100);

        // Assert - Should auto-save failure feedback with correct data
        MockFeedbackConfigurationService.Verify(
            x => x.SaveFailureFeedbackAsync(
                It.Is<ReaderFeedback>(feedback =>
                    feedback.BeepCount == 5 &&
                    feedback.Type == ReaderFeedbackType.Failure)),
            Times.AtLeastOnce);
    }

    #endregion

    #region Reset to Defaults Tests

    [Test]
    public void FeedbackConfiguration_ShowsResetToDefaultsButton()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Components.Configuration.FeedbackConfiguration>();

        // Assert
        var resetButton = component.Find("#reset-to-defaults-btn");
        Assert.That(resetButton, Is.Not.Null);
        Assert.That(resetButton.TextContent, Does.Contain("Reset to Defaults"));
    }

    [Test]
    public async Task FeedbackConfiguration_ResetButton_ShowsConfirmationModal()
    {
        // Arrange
        var component = RenderComponent<ApBox.Web.Components.Configuration.FeedbackConfiguration>();
        await Task.Delay(50); // Wait for initial load

        // Act
        var resetButton = component.Find("#reset-to-defaults-btn");
        await resetButton.ClickAsync(new MouseEventArgs());

        // Assert - Modal should be visible
        var modal = component.Find("#reset-confirmation-modal");
        Assert.That(modal, Is.Not.Null);
        
        var modalTitle = component.Find(".modal-title");
        Assert.That(modalTitle.TextContent, Is.EqualTo("Reset to Default Configuration"));
    }

    [Test]
    public async Task FeedbackConfiguration_ResetConfirmation_CallsServiceAndUpdatesForm()
    {
        // Arrange
        var component = RenderComponent<ApBox.Web.Components.Configuration.FeedbackConfiguration>();
        await Task.Delay(50); // Wait for initial load

        // Act - Open modal and confirm reset
        var resetButton = component.Find("#reset-to-defaults-btn");
        await resetButton.ClickAsync(new MouseEventArgs());

        var confirmButton = component.Find("#reset-confirm-btn");
        await confirmButton.ClickAsync(new MouseEventArgs());

        // Wait for async operations
        await Task.Delay(100);

        // Assert - Should call SaveDefaultConfigurationAsync
        MockFeedbackConfigurationService.Verify(
            x => x.SaveDefaultConfigurationAsync(It.IsAny<FeedbackConfiguration>()), 
            Times.Once);

        // Assert - Form should be reset to default values
        var successLedColor = component.Find("#success-led-color");
        Assert.That(successLedColor.GetAttribute("value"), Does.Contain("Green"));

        var successDisplayMessage = component.Find("#success-display-message");
        Assert.That(successDisplayMessage.GetAttribute("value"), Is.EqualTo("ACCESS GRANTED"));
    }

    [Test]
    public async Task FeedbackConfiguration_ResetCancel_DoesNotCallService()
    {
        // Arrange
        var component = RenderComponent<ApBox.Web.Components.Configuration.FeedbackConfiguration>();
        await Task.Delay(50); // Wait for initial load

        // Act - Open modal and cancel
        var resetButton = component.Find("#reset-to-defaults-btn");
        await resetButton.ClickAsync(new MouseEventArgs());

        var cancelButton = component.Find("#reset-cancel-btn");
        await cancelButton.ClickAsync(new MouseEventArgs());

        // Wait for async operations
        await Task.Delay(100);

        // Assert - Should NOT call SaveDefaultConfigurationAsync (only the initial load call)
        MockFeedbackConfigurationService.Verify(
            x => x.SaveDefaultConfigurationAsync(It.IsAny<FeedbackConfiguration>()), 
            Times.Never);
    }

    #endregion

    private FeedbackConfiguration GetDefaultFeedbackConfiguration()
    {
        return new FeedbackConfiguration
        {
            SuccessFeedback = new ReaderFeedback
            {
                Type = ReaderFeedbackType.Success,
                LedColor = LedColor.Green,
                LedDuration = 1000,
                BeepCount = 1,
                DisplayMessage = "ACCESS GRANTED"
            },
            FailureFeedback = new ReaderFeedback
            {
                Type = ReaderFeedbackType.Failure,
                LedColor = LedColor.Red,
                LedDuration = 2000,
                BeepCount = 3,
                DisplayMessage = "ACCESS DENIED"
            },
            IdleState = new IdleStateFeedback
            {
                PermanentLedColor = LedColor.Blue,
                HeartbeatFlashColor = LedColor.Green
            }
        };
    }
}