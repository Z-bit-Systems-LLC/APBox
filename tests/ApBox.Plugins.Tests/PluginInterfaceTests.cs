using ApBox.Plugins;

namespace ApBox.Plugins.Tests;

[TestFixture]
public class PluginInterfaceTests
{
    [Test]
    public void CardReadEvent_ShouldInitializeWithDefaults()
    {
        var cardReadEvent = new CardReadEvent();
        
        Assert.That(cardReadEvent.ReaderId, Is.EqualTo(Guid.Empty));
        Assert.That(cardReadEvent.CardNumber, Is.Empty);
        Assert.That(cardReadEvent.BitLength, Is.EqualTo(0));
        Assert.That(cardReadEvent.ReaderName, Is.Empty);
        Assert.That(cardReadEvent.AdditionalData, Is.Not.Null);
        Assert.That(cardReadEvent.AdditionalData, Is.Empty);
    }
    
    [Test]
    public void CardReadResult_ShouldInitializeWithDefaults()
    {
        var result = new CardReadResult();
        
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Is.Empty);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data, Is.Empty);
    }
}