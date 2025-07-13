using Bunit;
using ApBox.Web.Pages;
using ApBox.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ApBox.Web.Tests.Pages;

[TestFixture]
[Category("UI")]
public class CardEventsPageTests : ApBoxTestContext
{
    [SetUp]
    public void Setup()
    {
        ResetMocks();
        SetupDefaultMocks();
    }

    [Test]
    public void CardEvents_RendersSuccessfully()
    {
        // Act
        var component = RenderComponent<CardEvents>();

        // Assert
        Assert.That(component, Is.Not.Null);
        Assert.That(component.Find("h1").TextContent, Does.Contain("Card Events"));
    }

    [Test]
    public void CardEvents_DisplaysPageHeader()
    {
        // Act
        var component = RenderComponent<CardEvents>();

        // Assert
        var pageTitle = component.Find("h1.display-4");
        Assert.That(pageTitle.TextContent, Is.EqualTo("Card Events"));
        
        var subtitle = component.Find("p.text-muted");
        Assert.That(subtitle.TextContent, Is.EqualTo("View and search card reader events"));
    }

    [Test]
    public void CardEvents_DisplaysSearchControls()
    {
        // Act
        var component = RenderComponent<CardEvents>();

        // Assert
        var searchInput = component.Find("input[placeholder*='Search']");
        Assert.That(searchInput, Is.Not.Null);

        var readerSelect = component.Find("select option:contains('All Readers')");
        Assert.That(readerSelect, Is.Not.Null);

        var refreshButton = component.Find("button:contains('Refresh')");
        Assert.That(refreshButton, Is.Not.Null);
    }

    [Test]
    public void CardEvents_DisplaysViewModeButtons()
    {
        // Act
        var component = RenderComponent<CardEvents>();

        // Assert
        var tableButton = component.Find("button:contains('Table')");
        Assert.That(tableButton, Is.Not.Null);

        var cardsButton = component.Find("button:contains('Cards')");
        Assert.That(cardsButton, Is.Not.Null);
    }

    [Test]
    public void CardEvents_DefaultsToTableView()
    {
        // Act
        var component = RenderComponent<CardEvents>();

        // Assert
        var tableButton = component.Find("button:contains('Table')");
        Assert.That(tableButton.ClassList, Does.Contain("btn-primary"));

        var cardsButton = component.Find("button:contains('Cards')");
        Assert.That(cardsButton.ClassList, Does.Contain("btn-outline-primary"));
    }

    [Test]
    public void CardEvents_SwitchesToCardsView_WhenCardsButtonClicked()
    {
        // Act
        var component = RenderComponent<CardEvents>();
        var cardsButton = component.Find("button:contains('Cards')");
        cardsButton.Click();

        // Assert
        var tableButton = component.Find("button:contains('Table')");
        Assert.That(tableButton.ClassList, Does.Contain("btn-outline-primary"));

        var cardsButtonAfterClick = component.Find("button:contains('Cards')");
        Assert.That(cardsButtonAfterClick.ClassList, Does.Contain("btn-primary"));
    }

    [Test]
    public void CardEvents_DisplaysEventsTable_InTableMode()
    {
        // Act
        var component = RenderComponent<CardEvents>();

        // Assert - Should be in table mode by default
        var table = component.Find("table");
        Assert.That(table, Is.Not.Null);

        // Check table headers
        var headers = component.FindAll("th");
        var headerTexts = headers.Select(h => h.TextContent).ToList();
        
        Assert.That(headerTexts, Does.Contain("Timestamp"));
        Assert.That(headerTexts, Does.Contain("Reader"));
        Assert.That(headerTexts, Does.Contain("Card Number"));
        Assert.That(headerTexts, Does.Contain("Bit Length"));
        Assert.That(headerTexts, Does.Contain("Status"));
    }

    [Test]
    public void CardEvents_DisplaysEventCards_InCardsMode()
    {
        // Act
        var component = RenderComponent<CardEvents>();
        var cardsButton = component.Find("button:contains('Cards')");
        cardsButton.Click();

        // Assert
        // In cards mode, should not show table
        var tables = component.FindAll("table");
        Assert.That(tables.Count, Is.EqualTo(0));

        // Should show card layout (check for specific card structure or sample events)
        var cardStructure = component.FindAll(".row > .col-md-6");
        // Since we generate sample events, there should be some cards
        Assert.That(cardStructure.Count, Is.GreaterThan(0));
    }

    [Test]
    public void CardEvents_LoadsDataOnInitialization()
    {
        // Act
        var component = RenderComponent<CardEvents>();

        // Assert - Verify service call was made to load readers
        MockReaderService.Verify(x => x.GetReadersAsync(), Times.Once);
    }

    [Test]
    public void CardEvents_DisplaysEventCount()
    {
        // Act
        var component = RenderComponent<CardEvents>();

        // Assert
        var eventsHeader = component.Find("h5:contains('Recent Events')");
        Assert.That(eventsHeader.TextContent, Does.Match(@"Recent Events \(\d+\)"));
    }

    [Test]
    public void CardEvents_RefreshButton_ReloadsData()
    {
        // Act
        var component = RenderComponent<CardEvents>();
        MockReaderService.Invocations.Clear(); // Clear previous invocations
        MockReaderService.Setup(x => x.GetReadersAsync())
            .ReturnsAsync(GetDefaultReaderConfigurations());

        var refreshButton = component.Find("button:contains('Refresh')");
        refreshButton.Click();

        // Assert - Verify service was called again
        MockReaderService.Verify(x => x.GetReadersAsync(), Times.Once);
    }

    [Test]
    public void CardEvents_SearchInput_IsBindable()
    {
        // Act
        var component = RenderComponent<CardEvents>();
        var searchInput = component.Find("input[placeholder*='Search']");

        // Change the input value
        searchInput.Change("12345");

        // Assert - Input should have the new value
        Assert.That(searchInput.GetAttribute("value"), Is.EqualTo("12345"));
    }

    [Test]
    public void CardEvents_ReaderFilter_PopulatesFromService()
    {
        // Act
        var component = RenderComponent<CardEvents>();

        // Assert - Should have options for each reader plus "All Readers"
        var selectOptions = component.FindAll("select option");
        var optionTexts = selectOptions.Select(o => o.TextContent).ToList();

        Assert.That(optionTexts, Does.Contain("All Readers"));
        Assert.That(optionTexts, Does.Contain("Test Reader 1"));
        Assert.That(optionTexts, Does.Contain("Test Reader 2"));
    }

    [Test]
    public void CardEvents_ShowsNoEventsMessage_WhenFiltersReturnEmpty()
    {
        // Act
        var component = RenderComponent<CardEvents>();
        
        // Set a search term that won't match any generated sample data
        var searchInput = component.Find("input[placeholder*='Search']");
        searchInput.Change("NonExistentCardNumber99999");

        // Assert - Should show "No Events Found" message
        var noEventsMessage = component.Find("h4:contains('No Events Found')");
        Assert.That(noEventsMessage, Is.Not.Null);

        var noEventsDescription = component.Find("p:contains('No card events match your current filters')");
        Assert.That(noEventsDescription, Is.Not.Null);
    }

    private static IEnumerable<ReaderConfiguration> GetDefaultReaderConfigurations()
    {
        return new List<ReaderConfiguration>
        {
            new ReaderConfiguration
            {
                ReaderId = Guid.NewGuid(),
                ReaderName = "Test Reader 1"
            },
            new ReaderConfiguration
            {
                ReaderId = Guid.NewGuid(),
                ReaderName = "Test Reader 2"
            }
        };
    }
}