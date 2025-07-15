using ApBox.Core.Models;
using ApBox.Core.Services;
using ApBox.Plugins;
using ApBox.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApBox.Web.Controllers;

/// <summary>
/// Handles card read events and provides statistics for card reader activity
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CardEventsController : ControllerBase
{
    private readonly ICardProcessingService _cardProcessingService;
    private readonly IEnhancedCardProcessingService _enhancedCardProcessingService;
    private readonly ILogger<CardEventsController> _logger;
    
    public CardEventsController(
        ICardProcessingService cardProcessingService,
        IEnhancedCardProcessingService enhancedCardProcessingService,
        ILogger<CardEventsController> logger)
    {
        _cardProcessingService = cardProcessingService;
        _enhancedCardProcessingService = enhancedCardProcessingService;
        _logger = logger;
    }
    
    /// <summary>
    /// Process a card read event manually (for testing)
    /// </summary>
    /// <param name="request">The card read event details to process</param>
    /// <returns>The processing result including success status and reader feedback</returns>
    /// <response code="200">Returns the processing result</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost("process")]
    [ProducesResponseType(typeof(CardProcessingResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CardProcessingResultDto>> ProcessCardRead([FromBody] ProcessCardRequest request)
    {
        try
        {
            var cardReadEvent = new CardReadEvent
            {
                ReaderId = request.ReaderId,
                CardNumber = request.CardNumber,
                BitLength = request.BitLength ?? 26,
                Timestamp = DateTime.UtcNow,
                ReaderName = request.ReaderName ?? "Manual Entry",
                AdditionalData = request.AdditionalData ?? new Dictionary<string, object>()
            };
            
            _logger.LogInformation("Processing manual card read for reader {ReaderId}, card {CardNumber}", 
                request.ReaderId, request.CardNumber);
            
            var result = await _cardProcessingService.ProcessCardReadAsync(cardReadEvent);
            var feedback = await _cardProcessingService.GetFeedbackAsync(request.ReaderId, result);
            
            var response = new CardProcessingResultDto
            {
                Success = result.Success,
                Message = result.Message,
                Feedback = feedback,
                ProcessedAt = DateTime.UtcNow,
                CardNumber = request.CardNumber,
                ReaderId = request.ReaderId
            };
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing card read for reader {ReaderId}", request.ReaderId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Process a card read event with real-time notifications
    /// </summary>
    /// <param name="request">The card read event details to process</param>
    /// <returns>The processing result with real-time notifications sent to connected clients</returns>
    /// <response code="200">Returns the processing result</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost("process-realtime")]
    [ProducesResponseType(typeof(CardProcessingResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CardProcessingResultDto>> ProcessCardReadRealtime([FromBody] ProcessCardRequest request)
    {
        try
        {
            var cardReadEvent = new CardReadEvent
            {
                ReaderId = request.ReaderId,
                CardNumber = request.CardNumber,
                BitLength = request.BitLength ?? 26,
                Timestamp = DateTime.UtcNow,
                ReaderName = request.ReaderName ?? "Manual Entry",
                AdditionalData = request.AdditionalData ?? new Dictionary<string, object>()
            };

            _logger.LogInformation("Processing real-time card read for reader {ReaderId}, card {CardNumber}", 
                request.ReaderId, request.CardNumber);

            // Use enhanced service for real-time processing and notifications
            var result = await _enhancedCardProcessingService.ProcessCardReadWithNotificationAsync(cardReadEvent);
            var feedback = await _enhancedCardProcessingService.GetFeedbackAsync(request.ReaderId, result);

            var response = new CardProcessingResultDto
            {
                Success = result.Success,
                Message = result.Message,
                Feedback = feedback,
                ProcessedAt = DateTime.UtcNow,
                CardNumber = request.CardNumber,
                ReaderId = request.ReaderId
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing real-time card read for reader {ReaderId}", request.ReaderId);
            return StatusCode(500, "Internal server error");
        }
    }
    
    /// <summary>
    /// Get card processing statistics
    /// </summary>
    /// <returns>Statistics including total events, success rates, and timing information</returns>
    /// <response code="200">Returns the card event statistics</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(CardEventStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<CardEventStatisticsDto> GetStatistics()
    {
        try
        {
            // This would typically come from a database or cache
            // For now, return mock statistics
            var statistics = new CardEventStatisticsDto
            {
                TotalEvents = 1250,
                SuccessfulEvents = 1180,
                FailedEvents = 70,
                EventsToday = 45,
                EventsThisWeek = 320,
                AverageProcessingTime = TimeSpan.FromMilliseconds(150),
                LastEventTime = DateTime.UtcNow.AddMinutes(-5)
            };
            
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving card event statistics");
            return StatusCode(500, "Internal server error");
        }
    }
}

/// <summary>
/// Request model for manually processing a card read event
/// </summary>
public class ProcessCardRequest
{
    /// <summary>
    /// The unique identifier of the reader that read the card
    /// </summary>
    public Guid ReaderId { get; set; }
    
    /// <summary>
    /// The card number/credential data
    /// </summary>
    public string CardNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// The bit length of the card data (default: 26)
    /// </summary>
    public int? BitLength { get; set; }
    
    /// <summary>
    /// Optional display name of the reader
    /// </summary>
    public string? ReaderName { get; set; }
    
    /// <summary>
    /// Additional metadata about the card read event
    /// </summary>
    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// Result of processing a card read event
/// </summary>
public class CardProcessingResultDto
{
    /// <summary>
    /// Indicates whether the card was successfully processed
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Processing result message
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Feedback to be sent to the reader (LED, beeps, display)
    /// </summary>
    public ReaderFeedback Feedback { get; set; } = new();
    
    /// <summary>
    /// Timestamp when the card was processed
    /// </summary>
    public DateTime ProcessedAt { get; set; }
    
    /// <summary>
    /// The card number that was processed
    /// </summary>
    public string CardNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// The reader that read the card
    /// </summary>
    public Guid ReaderId { get; set; }
}

/// <summary>
/// Statistics about card read events
/// </summary>
public class CardEventStatisticsDto
{
    /// <summary>
    /// Total number of card events processed
    /// </summary>
    public int TotalEvents { get; set; }
    
    /// <summary>
    /// Number of successfully processed events
    /// </summary>
    public int SuccessfulEvents { get; set; }
    
    /// <summary>
    /// Number of failed events
    /// </summary>
    public int FailedEvents { get; set; }
    
    /// <summary>
    /// Number of events processed today
    /// </summary>
    public int EventsToday { get; set; }
    
    /// <summary>
    /// Number of events processed this week
    /// </summary>
    public int EventsThisWeek { get; set; }
    
    /// <summary>
    /// Average time to process a card event
    /// </summary>
    public TimeSpan AverageProcessingTime { get; set; }
    
    /// <summary>
    /// Timestamp of the most recent card event
    /// </summary>
    public DateTime? LastEventTime { get; set; }
    
    /// <summary>
    /// Success rate as a percentage (0-100)
    /// </summary>
    public double SuccessRate => TotalEvents > 0 ? (double)SuccessfulEvents / TotalEvents * 100 : 0;
}