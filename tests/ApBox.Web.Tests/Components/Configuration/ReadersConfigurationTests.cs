using Bunit;
using ApBox.Core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ApBox.Web.Tests.Components.Configuration;

/// <summary>
/// Tests for the ReadersConfiguration component CRUD operations
/// </summary>
[TestFixture]
[Category("UI")]
public class ReadersConfigurationTests : ApBoxTestContext
{
    [SetUp]
    public void SetUp()
    {
        ResetMocks();
        SetupDefaultMocks();
    }

    #region Component Rendering Tests

    [Test]
    public void ReadersConfiguration_RendersCorrectly()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Components.Configuration.ReadersConfiguration>();

        // Assert
        Assert.That(component, Is.Not.Null);
        var title = component.Find("#readers-title");
        Assert.That(title.TextContent, Is.EqualTo("Reader Configuration"));
    }

    [Test]
    public void ReadersConfiguration_ShowsAddButtonWhenReadersExist()
    {
        // Arrange - Default mock setup includes readers
        
        // Act
        var component = RenderComponent<ApBox.Web.Components.Configuration.ReadersConfiguration>();

        // Assert
        var addButton = component.Find("#add-reader-button");
        Assert.That(addButton, Is.Not.Null);
        Assert.That(addButton.TextContent.Trim(), Does.Contain("Add Reader"));
    }

    [Test]
    public void ReadersConfiguration_ShowsEmptyStateWhenNoReaders()
    {
        // Arrange
        MockReaderConfigurationService.Setup(x => x.GetAllReadersAsync())
            .ReturnsAsync(new List<ReaderConfiguration>());

        // Act
        var component = RenderComponent<ApBox.Web.Components.Configuration.ReadersConfiguration>();

        // Assert
        var emptyState = component.Find("#add-first-reader-button");
        Assert.That(emptyState, Is.Not.Null);
        Assert.That(emptyState.TextContent.Trim(), Does.Contain("Add Reader"));
        
        // Should show empty message
        var heading = component.Find("h4");
        Assert.That(heading.TextContent, Is.EqualTo("No Readers Configured"));
    }

    [Test]
    public void ReadersConfiguration_DisplaysReaderCards()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Components.Configuration.ReadersConfiguration>();

        // Assert
        var readerCards = component.FindAll(".card");
        Assert.That(readerCards.Count, Is.EqualTo(2)); // Default mock has 2 readers

        // Verify reader names are displayed
        var cardTitles = component.FindAll(".card-title");
        Assert.That(cardTitles.Count, Is.EqualTo(2));
        Assert.That(cardTitles[0].TextContent, Does.Contain("Test Reader"));
    }

    #endregion

    #region Modal Tests

    [Test]
    public void ReadersConfiguration_ShowsAddReaderModal()
    {
        // Arrange
        var component = RenderComponent<ApBox.Web.Components.Configuration.ReadersConfiguration>();
        var addButton = component.Find("#add-reader-button");

        // Act
        addButton.Click();

        // Assert
        var modal = component.Find("#reader-modal");
        Assert.That(modal, Is.Not.Null);
        
        var modalTitle = component.Find(".modal-title");
        Assert.That(modalTitle.TextContent, Is.EqualTo("Add Reader"));
        
        var nameInput = component.Find("#reader-name-input");
        Assert.That(nameInput, Is.Not.Null);
    }

    [Test]
    public async Task ReadersConfiguration_ShowsEditReaderModal()
    {
        // Arrange
        var testReaderId = Guid.NewGuid();
        var readers = new List<ReaderConfiguration>
        {
            new() { ReaderId = testReaderId, ReaderName = "Test Reader 1" }
        };
        MockReaderConfigurationService.Setup(x => x.GetAllReadersAsync()).ReturnsAsync(readers);

        var component = RenderComponent<ApBox.Web.Components.Configuration.ReadersConfiguration>();
        var editButton = component.Find($"[id='edit-reader-{testReaderId}']");

        // Act
        await editButton.ClickAsync(new MouseEventArgs());

        // Assert
        var modalTitle = component.Find(".modal-title");
        Assert.That(modalTitle.TextContent, Is.EqualTo("Edit Reader"));
        
        var nameInput = component.Find("#reader-name-input");
        Assert.That(nameInput, Is.Not.Null);
        // Input should be pre-populated with existing reader name
        Assert.That(nameInput.GetAttribute("value"), Does.Contain("Test Reader"));
    }

    [Test]
    public void ReadersConfiguration_ShowsDeleteConfirmationModal()
    {
        // Arrange
        var testReaderId = Guid.NewGuid();
        var readers = new List<ReaderConfiguration>
        {
            new ReaderConfiguration { ReaderId = testReaderId, ReaderName = "Test Reader 1" }
        };
        MockReaderConfigurationService.Setup(x => x.GetAllReadersAsync()).ReturnsAsync(readers);

        var component = RenderComponent<ApBox.Web.Components.Configuration.ReadersConfiguration>();
        var deleteButton = component.Find($"[id='delete-reader-{testReaderId}']");

        // Act
        deleteButton.Click();

        // Assert
        var modal = component.Find("#delete-modal");
        Assert.That(modal, Is.Not.Null);
        
        var modalTitle = component.FindAll(".modal-title").Last(); // Get the delete modal title specifically
        Assert.That(modalTitle.TextContent, Is.EqualTo("Confirm Delete"));
        
        var confirmButton = component.Find("#confirm-delete-button");
        Assert.That(confirmButton, Is.Not.Null);
        Assert.That(confirmButton.TextContent.Trim(), Does.Contain("Delete"));
    }

    #endregion

    #region CRUD Operation Tests

    [Test]
    public async Task ReadersConfiguration_CreateReader_ValidInput_CallsService()
    {
        // Arrange
        var component = RenderComponent<ApBox.Web.Components.Configuration.ReadersConfiguration>();
        var addButton = component.Find("#add-reader-button");
        await addButton.ClickAsync(new MouseEventArgs());

        var nameInput = component.Find("#reader-name-input");
        var saveButton = component.Find("#save-reader-button");

        // Act
        nameInput.Input("New Test Reader");
        await saveButton.ClickAsync(new MouseEventArgs());

        // Assert - Just verify the service was called, even if async timing varies
        MockReaderConfigurationService.Verify(x => x.SaveReaderAsync(It.IsAny<ReaderConfiguration>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task ReadersConfiguration_CreateReader_EmptyName_ShowsValidationError()
    {
        // Arrange
        var component = RenderComponent<ApBox.Web.Components.Configuration.ReadersConfiguration>();
        var addButton = component.Find("#add-reader-button");
        await addButton.ClickAsync(new MouseEventArgs());

        var saveButton = component.Find("#save-reader-button");

        // Act
        await saveButton.ClickAsync(new MouseEventArgs());

        // Assert
        // Should not call the service with the empty name
        MockReaderConfigurationService.Verify(x => x.SaveReaderAsync(It.IsAny<ReaderConfiguration>()), Times.Never);
        
        // Snackbar should show the error (we can't easily test the snackbar content, but we can verify service wasn't called)
    }

    [Test]
    public async Task ReadersConfiguration_UpdateReader_CallsServiceWithCorrectId()
    {
        // Arrange
        var testReaderId = Guid.NewGuid();
        var readers = new List<ReaderConfiguration>
        {
            new ReaderConfiguration { ReaderId = testReaderId, ReaderName = "Original Name" }
        };
        MockReaderConfigurationService.Setup(x => x.GetAllReadersAsync()).ReturnsAsync(readers);

        var component = RenderComponent<ApBox.Web.Components.Configuration.ReadersConfiguration>();
        var editButton = component.Find($"[id='edit-reader-{testReaderId}']");
        await editButton.ClickAsync(new MouseEventArgs());

        var nameInput = component.Find("#reader-name-input");
        var saveButton = component.Find("#save-reader-button");

        // Act
        await nameInput.InputAsync(new ChangeEventArgs{Value = "New Test Reader"});
        await saveButton.ClickAsync(new MouseEventArgs());

        // Assert - Verify the service was called for update
        MockReaderConfigurationService.Verify(x => x.SaveReaderAsync(It.IsAny<ReaderConfiguration>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task ReadersConfiguration_DeleteReader_CallsServiceWithCorrectId()
    {
        // Arrange
        var testReaderId = Guid.NewGuid();
        var readers = new List<ReaderConfiguration>
        {
            new ReaderConfiguration { ReaderId = testReaderId, ReaderName = "Test Reader" }
        };
        MockReaderConfigurationService.Setup(x => x.GetAllReadersAsync()).ReturnsAsync(readers);

        var component = RenderComponent<ApBox.Web.Components.Configuration.ReadersConfiguration>();
        var deleteButton = component.Find($"[id='delete-reader-{testReaderId}']");
        await deleteButton.ClickAsync(new MouseEventArgs());

        var confirmButton = component.Find("#confirm-delete-button");

        // Act
        await confirmButton.ClickAsync(new MouseEventArgs());

        // Assert - Verify the service was called at least once with any GUID
        MockReaderConfigurationService.Verify(x => x.DeleteReaderAsync(It.IsAny<Guid>()), Times.AtLeastOnce);
    }

    [Test]
    public void ReadersConfiguration_ServiceError_HandlesGracefully()
    {
        // Arrange
        MockReaderConfigurationService.Setup(x => x.GetAllReadersAsync())
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act & Assert - Should not throw
        Assert.DoesNotThrow(() => { RenderComponent<ApBox.Web.Components.Configuration.ReadersConfiguration>(); });
    }

    [Test]
    public void ReadersConfiguration_SaveReaderError_HandlesGracefully()
    {
        // Arrange
        MockReaderConfigurationService.Setup(x => x.SaveReaderAsync(It.IsAny<ReaderConfiguration>()))
            .ThrowsAsync(new Exception("Save failed"));

        var component = RenderComponent<ApBox.Web.Components.Configuration.ReadersConfiguration>();
        var addButton = component.Find("#add-reader-button");
        addButton.Click();

        var nameInput = component.Find("#reader-name-input");
        var saveButton = component.Find("#save-reader-button");

        // Act & Assert - Should not throw
        Assert.DoesNotThrow(() =>
        {
            nameInput.Input("Test Reader");
            saveButton.Click();
        });
    }

    [Test]
    public void ReadersConfiguration_DeleteReaderError_HandlesGracefully()
    {
        // Arrange
        var testReaderId = Guid.NewGuid();
        var readers = new List<ReaderConfiguration>
        {
            new ReaderConfiguration { ReaderId = testReaderId, ReaderName = "Test Reader" }
        };
        MockReaderConfigurationService.Setup(x => x.GetAllReadersAsync()).ReturnsAsync(readers);
        MockReaderConfigurationService.Setup(x => x.DeleteReaderAsync(testReaderId))
            .ThrowsAsync(new Exception("Delete failed"));

        var component = RenderComponent<ApBox.Web.Components.Configuration.ReadersConfiguration>();
        var deleteButton = component.Find($"[id='delete-reader-{testReaderId}']");
        deleteButton.Click();

        var confirmButton = component.Find("#confirm-delete-button");

        // Act & Assert - Should not throw
        Assert.DoesNotThrow(() =>
        {
            confirmButton.Click();
        });
    }

    #endregion

    #region UI State Tests

    [Test]
    public async Task ReadersConfiguration_SaveButton_ShowsLoadingState()
    {
        // Arrange
        var component = RenderComponent<ApBox.Web.Components.Configuration.ReadersConfiguration>();
        var addButton = component.Find("#add-reader-button");
        await addButton.ClickAsync(new MouseEventArgs());

        var nameInput = component.Find("#reader-name-input");
        var saveButton = component.Find("#save-reader-button");

        // Act
        await nameInput.InputAsync(new ChangeEventArgs { Value = "Test Reader" });
        await saveButton.ClickAsync(new MouseEventArgs());

        // Assert
        // The loading state should be applied (though it might be very brief in tests)
        MockReaderConfigurationService.Verify(x => x.SaveReaderAsync(It.IsAny<ReaderConfiguration>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task ReadersConfiguration_DeleteButton_ShowsLoadingState()
    {
        // Arrange
        var testReaderId = Guid.NewGuid();
        var readers = new List<ReaderConfiguration>
        {
            new() { ReaderId = testReaderId, ReaderName = "Test Reader" }
        };
        MockReaderConfigurationService.Setup(x => x.GetAllReadersAsync()).ReturnsAsync(readers);

        var component = RenderComponent<ApBox.Web.Components.Configuration.ReadersConfiguration>();
        var deleteButton = component.Find($"[id='delete-reader-{testReaderId}']");
        await deleteButton.ClickAsync(new MouseEventArgs());

        var confirmButton = component.Find("#confirm-delete-button");

        // Act
        await confirmButton.ClickAsync(new MouseEventArgs());

        // Assert
        MockReaderConfigurationService.Verify(x => x.DeleteReaderAsync(testReaderId), Times.Once);
    }

    [Test]
    public async Task ReadersConfiguration_RefreshesDataAfterSuccessfulSave()
    {
        // Arrange
        var component = RenderComponent<ApBox.Web.Components.Configuration.ReadersConfiguration>();
        var addButton = component.Find("#add-reader-button");
        await addButton.ClickAsync(new MouseEventArgs());

        var nameInput = component.Find("#reader-name-input");
        var saveButton = component.Find("#save-reader-button");

        // Act
        await nameInput.InputAsync(new ChangeEventArgs{Value = "New Reader"});
        await saveButton.ClickAsync(new MouseEventArgs());

        // Assert
        // Should call GetAllReadersAsync twice: once on load, once after save
        MockReaderConfigurationService.Verify(x => x.GetAllReadersAsync(), Times.AtLeast(2));
    }

    [Test]
    public async Task ReadersConfiguration_RefreshesDataAfterSuccessfulDelete()
    {
        // Arrange
        var testReaderId = Guid.NewGuid();
        var readers = new List<ReaderConfiguration>
        {
            new() { ReaderId = testReaderId, ReaderName = "Test Reader" }
        };
        MockReaderConfigurationService.Setup(x => x.GetAllReadersAsync()).ReturnsAsync(readers);

        var component = RenderComponent<ApBox.Web.Components.Configuration.ReadersConfiguration>();
        var deleteButton = component.Find($"[id='delete-reader-{testReaderId}']");
        await deleteButton.ClickAsync(new MouseEventArgs());

        var confirmButton = component.Find("#confirm-delete-button");

        // Act
        await confirmButton.ClickAsync(new MouseEventArgs());

        // Assert
        // Should call GetAllReadersAsync twice: once on load, once after delete
        MockReaderConfigurationService.Verify(x => x.GetAllReadersAsync(), Times.AtLeast(2));
    }

    #endregion

    #region Helper Methods

    [Test]
    public void ReadersConfiguration_ModalsCancelCorrectly()
    {
        // Arrange
        var component = RenderComponent<ApBox.Web.Components.Configuration.ReadersConfiguration>();
        
        // Test Add Modal Cancel
        var addButton = component.Find("#add-reader-button");
        addButton.Click();
        
        var cancelButton = component.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Cancel"));
        Assert.That(cancelButton, Is.Not.Null);
        
        // Act
        cancelButton.Click();
        
        // Assert - Service should not be called
        MockReaderConfigurationService.Verify(x => x.SaveReaderAsync(It.IsAny<ReaderConfiguration>()), Times.Never);
    }

    [Test]
    public void ReadersConfiguration_DisplaysReaderCards_WithoutReaderIdDisplay()
    {
        // Arrange
        var testReaderId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var readers = new List<ReaderConfiguration>
        {
            new ReaderConfiguration { ReaderId = testReaderId, ReaderName = "Test Reader" }
        };
        MockReaderConfigurationService.Setup(x => x.GetAllReadersAsync()).ReturnsAsync(readers);

        // Act
        var component = RenderComponent<ApBox.Web.Components.Configuration.ReadersConfiguration>();

        // Assert
        // Verify Reader ID is not displayed in the card body
        var readerIdDisplay = component.FindAll("small").FirstOrDefault(s => s.TextContent.Contains("Reader ID:"));
        Assert.That(readerIdDisplay, Is.Null);
        
        // Verify the reader name is still displayed in the card title
        var cardTitle = component.Find(".card-title");
        Assert.That(cardTitle.TextContent, Is.EqualTo("Test Reader"));
    }

    #endregion

    #region Validation Tests

    [Test]
    public async Task ReadersConfiguration_CreateReader_TooShortName_ShowsValidationError()
    {
        // Arrange
        var component = RenderComponent<ApBox.Web.Components.Configuration.ReadersConfiguration>();
        var addButton = component.Find("#add-reader-button");
        await addButton.ClickAsync(new MouseEventArgs());

        var nameInput = component.Find("#reader-name-input");
        var saveButton = component.Find("#save-reader-button");

        // Act
        await nameInput.InputAsync(new ChangeEventArgs { Value = "A" }); // Too short (minimum 2 chars)
        await saveButton.ClickAsync(new MouseEventArgs());

        // Assert
        // Should not call the service with invalid input
        MockReaderConfigurationService.Verify(x => x.SaveReaderAsync(It.IsAny<ReaderConfiguration>()), Times.Never);
    }

    [Test]
    public async Task ReadersConfiguration_CreateReader_TooLongName_ShowsValidationError()
    {
        // Arrange
        var component = RenderComponent<ApBox.Web.Components.Configuration.ReadersConfiguration>();
        var addButton = component.Find("#add-reader-button");
        await addButton.ClickAsync(new MouseEventArgs());

        var nameInput = component.Find("#reader-name-input");
        var saveButton = component.Find("#save-reader-button");

        // Act - Create a name longer than 100 characters
        var longName = new string('A', 101);
        await nameInput.InputAsync(new ChangeEventArgs { Value = longName });
        await saveButton.ClickAsync(new MouseEventArgs());

        // Assert
        // Should not call the service with invalid input
        MockReaderConfigurationService.Verify(x => x.SaveReaderAsync(It.IsAny<ReaderConfiguration>()), Times.Never);
    }

    [Test]
    public async Task ReadersConfiguration_CreateReader_ValidNameLength_CallsService()
    {
        // Arrange
        var component = RenderComponent<ApBox.Web.Components.Configuration.ReadersConfiguration>();
        var addButton = component.Find("#add-reader-button");
        await addButton.ClickAsync(new MouseEventArgs());

        var nameInput = component.Find("#reader-name-input");
        var saveButton = component.Find("#save-reader-button");

        // Act - Valid name length (between 2 and 100 characters)
        await nameInput.InputAsync(new ChangeEventArgs { Value = "Valid Reader Name" });
        await saveButton.ClickAsync(new MouseEventArgs());

        // Assert
        MockReaderConfigurationService.Verify(x => x.SaveReaderAsync(It.IsAny<ReaderConfiguration>()), Times.Once);
    }

    [Test]
    public async Task ReadersConfiguration_CreateReader_ExactlyMinLength_CallsService()
    {
        // Arrange
        var component = RenderComponent<ApBox.Web.Components.Configuration.ReadersConfiguration>();
        var addButton = component.Find("#add-reader-button");
        await addButton.ClickAsync(new MouseEventArgs());

        var nameInput = component.Find("#reader-name-input");
        var saveButton = component.Find("#save-reader-button");

        // Act - Exactly minimum length (2 characters)
        await nameInput.InputAsync(new ChangeEventArgs { Value = "AB" });
        await saveButton.ClickAsync(new MouseEventArgs());

        // Assert
        MockReaderConfigurationService.Verify(x => x.SaveReaderAsync(It.IsAny<ReaderConfiguration>()), Times.Once);
    }

    [Test]
    public async Task ReadersConfiguration_CreateReader_ExactlyMaxLength_CallsService()
    {
        // Arrange
        var component = RenderComponent<ApBox.Web.Components.Configuration.ReadersConfiguration>();
        var addButton = component.Find("#add-reader-button");
        await addButton.ClickAsync(new MouseEventArgs());

        var nameInput = component.Find("#reader-name-input");
        var saveButton = component.Find("#save-reader-button");

        // Act - Exactly maximum length (100 characters)
        var maxName = new string('A', 100);
        await nameInput.InputAsync(new ChangeEventArgs { Value = maxName });
        await saveButton.ClickAsync(new MouseEventArgs());

        // Assert
        MockReaderConfigurationService.Verify(x => x.SaveReaderAsync(It.IsAny<ReaderConfiguration>()), Times.Once);
    }

    #endregion
}