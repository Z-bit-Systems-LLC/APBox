using ApBox.Plugins;
using Bunit;

namespace ApBox.Web.Tests.Pages;

public class TestCardReadsPageTests : ApBoxTestContext
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
                ReaderId = Guid.NewGuid(),
                ReaderName = "Test Reader 1"
            },
            new ReaderConfiguration
            {
                ReaderId = Guid.NewGuid(),
                ReaderName = "Test Reader 2"
            }
        };

        MockReaderService.Setup(x => x.GetReadersAsync())
            .ReturnsAsync(readers);
    }

    [Test]
    public void TestCardReadsPage_ShouldRenderCorrectly()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.TestCardReads>();

        // Assert
        Assert.That(component.Find("h1").TextContent, Is.EqualTo("Test Card Reads"));
        Assert.That(component.Find("p").TextContent, Contains.Substring("Simulate card reads"));
    }

    [Test]
    public void TestCardReadsPage_ShouldDisplayReaderDropdown()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.TestCardReads>();

        // Assert
        var select = component.Find("#readerSelect");
        Assert.That(select, Is.Not.Null);
        
        var options = component.FindAll("option");
        Assert.That(options.Count, Is.GreaterThan(1)); // Should include default option + readers
    }

    [Test]
    public void TestCardReadsPage_ShouldDisplayCardNumberInput()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.TestCardReads>();

        // Assert
        var input = component.Find("#cardNumber");
        Assert.That(input, Is.Not.Null);
        Assert.That(input.GetAttribute("placeholder"), Is.EqualTo("Enter card number"));
    }

    [Test]
    public void TestCardReadsPage_ShouldDisplayBitLengthSelect()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.TestCardReads>();

        // Assert
        var select = component.Find("#bitLength");
        Assert.That(select, Is.Not.Null);
        
        var options = select.QuerySelectorAll("option");
        Assert.That(options.Length, Is.EqualTo(2)); // 26-bit and 37-bit
    }

    [Test]
    public void TestCardReadsPage_ShouldDisplaySimulateButton()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.TestCardReads>();

        // Assert
        var button = component.Find("button[type='submit']");
        Assert.That(button, Is.Not.Null);
        Assert.That(button.TextContent.Trim(), Is.EqualTo("Simulate Card Read"));
    }

    [Test]
    public void TestCardReadsPage_ShouldDisplayGenerateRandomButton()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.TestCardReads>();

        // Assert
        var button = component.Find("button:contains('Generate Random')");
        Assert.That(button, Is.Not.Null);
    }

    [Test]
    public void TestCardReadsPage_ShouldDisplayQuickActionButtons()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.TestCardReads>();

        // Assert
        var buttons = component.FindAll(".btn-outline-primary, .btn-outline-success, .btn-outline-info, .btn-outline-danger");
        Assert.That(buttons.Count, Is.EqualTo(4));
    }

    [Test]
    public void TestCardReadsPage_GenerateRandomButton_ShouldPopulateCardNumber()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.TestCardReads>();
        var cardNumberInput = component.Find("#cardNumber");
        var generateButton = component.Find("button:contains('Generate Random')");

        // Verify initial state
        Assert.That(cardNumberInput.GetAttribute("value"), Is.Empty.Or.Null);

        // Click generate button
        generateButton.Click();

        // Assert
        var cardNumberValue = cardNumberInput.GetAttribute("value");
        Assert.That(cardNumberValue, Is.Not.Empty);
        Assert.That(cardNumberValue.Length, Is.GreaterThanOrEqualTo(7)); // Should be at least 7 digits
    }

    [Test]
    public void TestCardReadsPage_ShouldHaveCorrectDefaultValues()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.TestCardReads>();

        // Assert
        var bitLengthSelect = component.Find("#bitLength");
        Assert.That(bitLengthSelect.GetAttribute("value"), Is.EqualTo("26"));
    }

    [Test]
    public void TestCardReadsPage_ShouldLoadReadersOnInitialization()
    {
        // Act
        RenderComponent<ApBox.Web.Pages.TestCardReads>();

        // Assert - Verify the service was called
        MockReaderService.Verify(x => x.GetReadersAsync(), Times.Once);
    }

    [Test]
    public void TestCardReadsPage_ShouldDisplayNoLastResultInitially()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.TestCardReads>();

        // Assert
        var alertElements = component.FindAll(".alert-info");
        Assert.That(alertElements.Count, Is.EqualTo(0));
    }

    [Test]
    public void TestCardReadsPage_ContinuousSimulationButtons_ShouldBePresent()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.TestCardReads>();

        // Assert
        var startButton = component.Find("button:contains('Start Continuous Simulation')");
        var stopButton = component.Find("button:contains('Stop Continuous')");
        
        Assert.That(startButton, Is.Not.Null);
        Assert.That(stopButton, Is.Not.Null);
        
        // Stop button should be disabled initially
        Assert.That(stopButton.HasAttribute("disabled"));
    }
}