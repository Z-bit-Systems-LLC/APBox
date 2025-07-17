using Bunit;
using ApBox.Web.Pages;
using ApBox.Core.Models;

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
        var title = component.Find("#card-events-title");
        Assert.That(title.TextContent, Does.Contain("Card Events"));
    }

    [Test]
    public void CardEvents_DisplaysPageHeader()
    {
        // Act
        var component = RenderComponent<CardEvents>();

        // Assert
        var pageTitle = component.Find("#card-events-title");
        Assert.That(pageTitle.TextContent, Is.EqualTo("Card Events"));
        
        var subtitle = component.Find("#card-events-subtitle");
        Assert.That(subtitle.TextContent, Is.EqualTo("View and search card reader events"));
    }

    [Test]
    public void CardEvents_DisplaysSearchControls()
    {
        // Act
        var component = RenderComponent<CardEvents>();

        // Assert
        var searchInput = component.Find("#search-input");
        Assert.That(searchInput, Is.Not.Null);

        var readerFilter = component.Find("#reader-filter");
        Assert.That(readerFilter, Is.Not.Null);

        var refreshButton = component.Find("#refresh-button");
        Assert.That(refreshButton, Is.Not.Null);
    }


    [Test]
    public void CardEvents_DisplaysEventsTable()
    {
        // Act
        var component = RenderComponent<CardEvents>();

        // Assert
        var table = component.Find("#events-table");
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
    public void CardEvents_LoadsDataOnInitialization()
    {
        // Act
        RenderComponent<CardEvents>();

        // Assert - Verify service call was made to load readers
        MockReaderService.Verify(x => x.GetReadersAsync(), Times.Once);
    }

    [Test]
    public void CardEvents_DisplaysEventCount()
    {
        // Act
        var component = RenderComponent<CardEvents>();

        // Assert
        var eventsHeader = component.Find("#events-count");
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

        var refreshButton = component.Find("#refresh-button");
        refreshButton.Click();

        // Assert - Verify service was called again
        MockReaderService.Verify(x => x.GetReadersAsync(), Times.Once);
    }

    [Test]
    public void CardEvents_SearchInput_IsBindable()
    {
        // Act
        var component = RenderComponent<CardEvents>();
        var searchInput = component.Find("#search-input");

        // Set the input value and trigger keyup event
        searchInput.Input("12345");

        // Assert - Input should have the new value
        Assert.That(searchInput.GetAttribute("value"), Is.EqualTo("12345"));
    }

    [Test]
    public void CardEvents_ReaderFilter_PopulatesFromService()
    {
        // Act
        var component = RenderComponent<CardEvents>();

        // Assert - Should have options for each reader plus "All Readers"
        var readerFilter = component.Find("#reader-filter");
        var selectOptions = readerFilter.QuerySelectorAll("option");
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
        var searchInput = component.Find("#search-input");
        searchInput.Input("NonExistentCardNumber99999");

        // Assert - Should show "No Events Found" message
        var noEventsMessage = component.Find("#no-events-message");
        Assert.That(noEventsMessage, Is.Not.Null);
        Assert.That(noEventsMessage.TextContent, Does.Contain("No Events Found"));
        Assert.That(noEventsMessage.TextContent, Does.Contain("No card events match your current filters"));
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