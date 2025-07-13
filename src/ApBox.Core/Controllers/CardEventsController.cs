using ApBox.Core.Services;
using ApBox.Plugins;
using Microsoft.AspNetCore.Mvc;

namespace ApBox.Core.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CardEventsController : ControllerBase
{
    private readonly ICardProcessingService _cardProcessingService;
    private readonly ILogger<CardEventsController> _logger;
    
    public CardEventsController(
        ICardProcessingService cardProcessingService,
        ILogger<CardEventsController> logger)
    {
        _cardProcessingService = cardProcessingService;
        _logger = logger;
    }
    
    /// <summary>
    /// Process a card read event manually (for testing)
    /// </summary>
    [HttpPost("process")]
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
    /// Get card processing statistics
    /// </summary>
    [HttpGet("statistics")]
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

public class ProcessCardRequest
{
    public Guid ReaderId { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public int? BitLength { get; set; }
    public string? ReaderName { get; set; }
    public Dictionary<string, object>? AdditionalData { get; set; }
}

public class CardProcessingResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ReaderFeedback Feedback { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public Guid ReaderId { get; set; }
}

public class CardEventStatisticsDto
{
    public int TotalEvents { get; set; }
    public int SuccessfulEvents { get; set; }
    public int FailedEvents { get; set; }
    public int EventsToday { get; set; }
    public int EventsThisWeek { get; set; }
    public TimeSpan AverageProcessingTime { get; set; }
    public DateTime? LastEventTime { get; set; }
    
    public double SuccessRate => TotalEvents > 0 ? (double)SuccessfulEvents / TotalEvents * 100 : 0;
}