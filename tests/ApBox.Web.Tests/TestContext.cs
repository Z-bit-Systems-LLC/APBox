using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ApBox.Core.Services;
using ApBox.Plugins;
using Moq;

namespace ApBox.Web.Tests;

public class ApBoxTestContext : Bunit.TestContext
{
    public Mock<IReaderService> MockReaderService { get; private set; }
    public Mock<ICardProcessingService> MockCardProcessingService { get; private set; }
    public Mock<IPluginLoader> MockPluginLoader { get; private set; }
    public Mock<IReaderConfigurationService> MockReaderConfigurationService { get; private set; }

    public ApBoxTestContext()
    {
        // Create mocks
        MockReaderService = new Mock<IReaderService>();
        MockCardProcessingService = new Mock<ICardProcessingService>();
        MockPluginLoader = new Mock<IPluginLoader>();
        MockReaderConfigurationService = new Mock<IReaderConfigurationService>();

        // Register mocked services
        Services.AddSingleton(MockReaderService.Object);
        Services.AddSingleton(MockCardProcessingService.Object);
        Services.AddSingleton(MockPluginLoader.Object);
        Services.AddSingleton(MockReaderConfigurationService.Object);

        // Add other required services
        Services.AddLogging();
    }

    public void ResetMocks()
    {
        MockReaderService.Reset();
        MockCardProcessingService.Reset();
        MockPluginLoader.Reset();
        MockReaderConfigurationService.Reset();
    }

    public void SetupDefaultMocks()
    {
        // Setup default behaviors for mocks
        MockReaderService.Setup(x => x.GetReadersAsync())
            .ReturnsAsync(GetDefaultReaderConfigurations());

        MockPluginLoader.Setup(x => x.LoadPluginsAsync())
            .ReturnsAsync(GetDefaultPlugins());

        MockCardProcessingService.Setup(x => x.ProcessCardReadAsync(It.IsAny<CardReadEvent>()))
            .ReturnsAsync(new CardReadResult { Success = true, Message = "Success" });
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

    private static IEnumerable<IApBoxPlugin> GetDefaultPlugins()
    {
        var mockPlugin = new Mock<IApBoxPlugin>();
        mockPlugin.Setup(x => x.Name).Returns("Test Plugin");
        mockPlugin.Setup(x => x.Version).Returns("1.0.0");
        mockPlugin.Setup(x => x.Description).Returns("Test plugin for unit tests");

        return new List<IApBoxPlugin> { mockPlugin.Object };
    }
}