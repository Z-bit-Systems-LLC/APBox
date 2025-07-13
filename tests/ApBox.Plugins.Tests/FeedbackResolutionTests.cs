using ApBox.Plugins;

namespace ApBox.Plugins.Tests;

[TestFixture]
public class FeedbackResolutionTests
{
    private FeedbackResolutionService _service;
    
    [SetUp]
    public void Setup()
    {
        _service = new FeedbackResolutionService();
    }
    
    [Test]
    public async Task ResolveFeedback_WithNoPlugin_ReturnsDefaultSuccessFeedback()
    {
        var readerId = Guid.NewGuid();
        var result = new CardReadResult { Success = true };
        
        var feedback = await _service.ResolveFeedbackAsync(readerId, result);
        
        Assert.That(feedback, Is.Not.Null);
        Assert.That(feedback.Type, Is.EqualTo(ReaderFeedbackType.Success));
    }
    
    [Test]
    public async Task ResolveFeedback_WithNoPlugin_ReturnsDefaultFailureFeedback()
    {
        var readerId = Guid.NewGuid();
        var result = new CardReadResult { Success = false };
        
        var feedback = await _service.ResolveFeedbackAsync(readerId, result);
        
        Assert.That(feedback, Is.Not.Null);
        Assert.That(feedback.Type, Is.EqualTo(ReaderFeedbackType.Failure));
    }
    
    [Test]
    public async Task ResolveFeedback_WithPlugin_ReturnsPluginFeedback()
    {
        var readerId = Guid.NewGuid();
        var result = new CardReadResult { Success = true };
        var expectedFeedback = new ReaderFeedback 
        { 
            Type = ReaderFeedbackType.Custom,
            BeepCount = 3,
            LedColor = LedColor.Green
        };
        
        var mockPlugin = new MockPlugin(expectedFeedback);
        
        var feedback = await _service.ResolveFeedbackAsync(readerId, result, mockPlugin);
        
        Assert.That(feedback, Is.EqualTo(expectedFeedback));
    }
    
    [Test]
    public async Task ResolveFeedback_WithPluginReturningNull_FallsBackToDefault()
    {
        var readerId = Guid.NewGuid();
        var result = new CardReadResult { Success = true };
        
        var mockPlugin = new MockPlugin(null);
        
        var feedback = await _service.ResolveFeedbackAsync(readerId, result, mockPlugin);
        
        Assert.That(feedback, Is.Not.Null);
        Assert.That(feedback.Type, Is.EqualTo(ReaderFeedbackType.Success));
    }
    
    private class MockPlugin : IApBoxPlugin
    {
        private readonly ReaderFeedback? _feedbackToReturn;
        
        public MockPlugin(ReaderFeedback? feedbackToReturn)
        {
            _feedbackToReturn = feedbackToReturn;
        }
        
        public string Name => "Mock Plugin";
        public string Version => "1.0.0";
        public string Description => "Mock plugin for testing";
        
        public Task<bool> ProcessCardReadAsync(CardReadEvent cardRead) => Task.FromResult(true);
        public Task<ReaderFeedback?> GetFeedbackAsync(CardReadResult result) => Task.FromResult(_feedbackToReturn);
        public Task InitializeAsync() => Task.CompletedTask;
        public Task ShutdownAsync() => Task.CompletedTask;
    }
}