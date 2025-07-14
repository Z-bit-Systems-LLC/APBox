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
        var title = component.Find("#test-card-reads-title");
        Assert.That(title.TextContent, Is.EqualTo("Test Card Reads"));
        
        var subtitle = component.Find("#test-card-reads-subtitle");
        Assert.That(subtitle.TextContent, Contains.Substring("Simulate card reads"));
    }

    [Test]
    public void TestCardReadsPage_ShouldDisplayReaderDropdown()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.TestCardReads>();

        // Assert
        var select = component.Find("#reader-select");
        Assert.That(select, Is.Not.Null);
        
        var options = select.QuerySelectorAll("option");
        Assert.That(options.Length, Is.GreaterThan(1)); // Should include default option + readers
    }

    [Test]
    public void TestCardReadsPage_ShouldDisplayCardNumberInput()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.TestCardReads>();

        // Assert
        var input = component.Find("#card-number-input");
        Assert.That(input, Is.Not.Null);
        Assert.That(input.GetAttribute("placeholder"), Is.EqualTo("Enter card number"));
    }

    [Test]
    public void TestCardReadsPage_ShouldDisplayBitLengthSelect()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.TestCardReads>();

        // Assert
        var select = component.Find("#bit-length-select");
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
        var button = component.Find("#simulate-button");
        Assert.That(button, Is.Not.Null);
        Assert.That(button.TextContent.Trim(), Is.EqualTo("Simulate Card Read"));
    }

    [Test]
    public void TestCardReadsPage_ShouldDisplayGenerateRandomButton()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.TestCardReads>();

        // Assert
        var button = component.Find("#generate-random-button");
        Assert.That(button, Is.Not.Null);
    }

    [Test]
    public void TestCardReadsPage_ShouldDisplayQuickActionButtons()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.TestCardReads>();

        // Assert
        var simulate5Button = component.Find("#simulate-5-button");
        var simulate10Button = component.Find("#simulate-10-button");
        var startContinuousButton = component.Find("#start-continuous-button");
        var stopContinuousButton = component.Find("#stop-continuous-button");
        
        Assert.That(simulate5Button, Is.Not.Null);
        Assert.That(simulate10Button, Is.Not.Null);
        Assert.That(startContinuousButton, Is.Not.Null);
        Assert.That(stopContinuousButton, Is.Not.Null);
    }

    [Test]
    public void TestCardReadsPage_GenerateRandomButton_ShouldPopulateCardNumber()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.TestCardReads>();
        var cardNumberInput = component.Find("#card-number-input");
        var generateButton = component.Find("#generate-random-button");

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
        var bitLengthSelect = component.Find("#bit-length-select");
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
        var alertElements = component.FindAll("#last-result-alert");
        Assert.That(alertElements.Count, Is.EqualTo(0));
    }

    [Test]
    public void TestCardReadsPage_ContinuousSimulationButtons_ShouldBePresent()
    {
        // Act
        var component = RenderComponent<ApBox.Web.Pages.TestCardReads>();

        // Assert
        var startButton = component.Find("#start-continuous-button");
        var stopButton = component.Find("#stop-continuous-button");
        
        Assert.That(startButton, Is.Not.Null);
        Assert.That(stopButton, Is.Not.Null);
        
        // Stop button should be disabled initially
        Assert.That(stopButton.HasAttribute("disabled"));
    }
}