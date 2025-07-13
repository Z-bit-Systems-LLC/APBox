using Bunit;
using ApBox.Web.Pages;
using ApBox.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ApBox.Web.Tests.Pages;

[TestFixture]
[Category("UI")]
public class ReadersPageTests : ApBoxTestContext
{
    [SetUp]
    public void Setup()
    {
        ResetMocks();
        SetupDefaultMocks();
    }

    [Test]
    public void Readers_RendersSuccessfully()
    {
        // Act
        var component = RenderComponent<Readers>();

        // Assert
        Assert.That(component, Is.Not.Null);
        Assert.That(component.Find("h1").TextContent, Does.Contain("Card Readers"));
    }

    [Test]
    public void Readers_DisplaysPageHeader()
    {
        // Act
        var component = RenderComponent<Readers>();

        // Assert
        var pageTitle = component.Find("h1.display-4");
        Assert.That(pageTitle.TextContent, Is.EqualTo("Card Readers"));
        
        var subtitle = component.Find("p.text-muted");
        Assert.That(subtitle.TextContent, Is.EqualTo("Manage OSDP card readers and their configurations"));
    }

    [Test]
    public void Readers_DisplaysAddReaderButton()
    {
        // Act
        var component = RenderComponent<Readers>();

        // Assert
        var addButton = component.Find("button:contains('Add Reader')");
        Assert.That(addButton, Is.Not.Null);
        Assert.That(addButton.ClassList, Does.Contain("btn-primary"));
    }

    [Test]
    public void Readers_DisplaysReaderCards()
    {
        // Act
        var component = RenderComponent<Readers>();

        // Assert - Should display reader cards for the 2 mock readers
        var readerCards = component.FindAll(".card.fade-in");
        Assert.That(readerCards.Count, Is.GreaterThanOrEqualTo(2));

        // Check for reader names
        var reader1Card = component.Find("h5:contains('Test Reader 1')");
        Assert.That(reader1Card, Is.Not.Null);

        var reader2Card = component.Find("h5:contains('Test Reader 2')");
        Assert.That(reader2Card, Is.Not.Null);
    }

    [Test]
    public void Readers_DisplaysReaderActionButtons()
    {
        // Act
        var component = RenderComponent<Readers>();

        // Assert - Each reader card should have Test, Configure, and Remove buttons
        var testButtons = component.FindAll("button:contains('Test')");
        Assert.That(testButtons.Count, Is.GreaterThanOrEqualTo(2));

        var configureButtons = component.FindAll("button:contains('Configure')");
        Assert.That(configureButtons.Count, Is.GreaterThanOrEqualTo(2));

        var removeButtons = component.FindAll("button:contains('Remove')");
        Assert.That(removeButtons.Count, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void Readers_ShowsAddReaderModal_WhenAddButtonClicked()
    {
        // Act
        var component = RenderComponent<Readers>();
        var addButton = component.Find("button:contains('Add Reader')");
        addButton.Click();

        // Assert
        var modal = component.Find(".modal");
        Assert.That(modal, Is.Not.Null);
        Assert.That(modal.ClassList, Does.Contain("show"));

        var modalTitle = component.Find(".modal-title");
        Assert.That(modalTitle.TextContent, Is.EqualTo("Add New Reader"));
    }

    [Test]
    public void Readers_AddReaderModal_ContainsRequiredFields()
    {
        // Act
        var component = RenderComponent<Readers>();
        var addButton = component.Find("button:contains('Add Reader')");
        addButton.Click();

        // Assert
        var nameInput = component.Find("input[id='readerName']");
        Assert.That(nameInput, Is.Not.Null);

        var addressInput = component.Find("input[id='readerAddress']");
        Assert.That(addressInput, Is.Not.Null);

        var descriptionInput = component.Find("textarea[id='readerDescription']");
        Assert.That(descriptionInput, Is.Not.Null);

        var cancelButton = component.Find("button:contains('Cancel')");
        Assert.That(cancelButton, Is.Not.Null);

        var addReaderButton = component.Find("button:contains('Add Reader')");
        Assert.That(addReaderButton, Is.Not.Null);
    }

    [Test]
    public void Readers_AddReaderButton_DisabledWhenNameEmpty()
    {
        // Act
        var component = RenderComponent<Readers>();
        var addButton = component.Find("button:contains('Add Reader')");
        addButton.Click();

        // Assert - Add Reader button should be disabled when name is empty
        var addReaderButton = component.FindAll("button:contains('Add Reader')").Last();
        Assert.That(addReaderButton.HasAttribute("disabled"), Is.True);
    }

    [Test]
    public void Readers_ClosesModal_WhenCancelClicked()
    {
        // Act
        var component = RenderComponent<Readers>();
        var addButton = component.Find("button:contains('Add Reader')");
        addButton.Click();
        
        var cancelButton = component.Find("button:contains('Cancel')");
        cancelButton.Click();

        // Assert - Modal should be hidden
        var modals = component.FindAll(".modal.show");
        Assert.That(modals.Count, Is.EqualTo(0));
    }

    [Test]
    public void Readers_LoadsReadersOnInitialization()
    {
        // Act
        var component = RenderComponent<Readers>();

        // Assert - Verify service call was made
        MockReaderService.Verify(x => x.GetReadersAsync(), Times.Once);
    }

    [Test]
    public void Readers_ShowsEmptyState_WhenNoReaders()
    {
        // Arrange
        MockReaderService.Setup(x => x.GetReadersAsync())
            .ReturnsAsync(new List<ReaderConfiguration>());

        // Act
        var component = RenderComponent<Readers>();

        // Assert
        var emptyStateMessage = component.Find("h3:contains('No Readers Configured')");
        Assert.That(emptyStateMessage, Is.Not.Null);

        var emptyStateDescription = component.Find("p:contains('Add your first card reader to get started')");
        Assert.That(emptyStateDescription, Is.Not.Null);
    }

    [Test]
    public void Readers_ReaderCard_DisplaysCorrectInformation()
    {
        // Act
        var component = RenderComponent<Readers>();

        // Assert - Check first reader card content
        var firstCard = component.FindAll(".card.fade-in").First();
        
        // Should have reader name as title
        var titleElement = firstCard.QuerySelector("h5.card-title");
        Assert.That(titleElement?.TextContent, Does.Contain("Test Reader"));

        // Should show online status
        var statusBadge = firstCard.QuerySelector(".badge.bg-success");
        Assert.That(statusBadge?.TextContent, Is.EqualTo("Online"));

        // Should have ID information
        var cardText = firstCard.QuerySelector(".card-text");
        Assert.That(cardText?.TextContent, Does.Contain("ID:"));
        Assert.That(cardText?.TextContent, Does.Contain("Address:"));
        Assert.That(cardText?.TextContent, Does.Contain("Type:"));
    }
}