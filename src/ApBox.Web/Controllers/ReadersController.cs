using ApBox.Core.OSDP;
using ApBox.Core.Services;
using ApBox.Plugins;
using Microsoft.AspNetCore.Mvc;

namespace ApBox.Web.Controllers;

/// <summary>
/// Manages OSDP card readers including configuration, status monitoring, and feedback control
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ReadersController : ControllerBase
{
    private readonly IReaderService _readerService;
    private readonly IOsdpCommunicationManager _osdpManager;
    private readonly ILogger<ReadersController> _logger;
    
    public ReadersController(
        IReaderService readerService,
        IOsdpCommunicationManager osdpManager,
        ILogger<ReadersController> logger)
    {
        _readerService = readerService;
        _osdpManager = osdpManager;
        _logger = logger;
    }
    
    /// <summary>
    /// Get all configured readers
    /// </summary>
    /// <returns>A list of all configured readers with their current status</returns>
    /// <response code="200">Returns the list of readers</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ReaderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ReaderDto>>> GetReaders()
    {
        try
        {
            var readers = await _readerService.GetReadersAsync();
            var osdpDevices = await _osdpManager.GetDevicesAsync();
            
            var readerDtos = readers.Select(r => new ReaderDto
            {
                Id = r.ReaderId,
                Name = r.ReaderName,
                IsOnline = osdpDevices.FirstOrDefault(d => d.Id == r.ReaderId)?.IsOnline ?? false,
                LastActivity = osdpDevices.FirstOrDefault(d => d.Id == r.ReaderId)?.LastActivity,
                DefaultFeedback = r.DefaultFeedback,
                ResultFeedback = r.ResultFeedback
            });
            
            return Ok(readerDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving readers");
            return StatusCode(500, "Internal server error");
        }
    }
    
    /// <summary>
    /// Get a specific reader by ID
    /// </summary>
    /// <param name="id">The unique identifier of the reader</param>
    /// <returns>The reader details if found</returns>
    /// <response code="200">Returns the requested reader</response>
    /// <response code="404">If the reader was not found</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ReaderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ReaderDto>> GetReader(Guid id)
    {
        try
        {
            var reader = await _readerService.GetReaderAsync(id);
            if (reader == null)
            {
                return NotFound($"Reader with ID {id} not found");
            }
            
            var osdpDevice = await _osdpManager.GetDeviceAsync(id);
            
            var readerDto = new ReaderDto
            {
                Id = reader.ReaderId,
                Name = reader.ReaderName,
                IsOnline = osdpDevice?.IsOnline ?? false,
                LastActivity = osdpDevice?.LastActivity,
                DefaultFeedback = reader.DefaultFeedback,
                ResultFeedback = reader.ResultFeedback
            };
            
            return Ok(readerDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reader {ReaderId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
    
    /// <summary>
    /// Update reader configuration
    /// </summary>
    /// <param name="id">The unique identifier of the reader to update</param>
    /// <param name="request">The updated reader configuration</param>
    /// <returns>No content on success</returns>
    /// <response code="204">The reader was successfully updated</response>
    /// <response code="404">If the reader was not found</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> UpdateReader(Guid id, [FromBody] UpdateReaderRequest request)
    {
        try
        {
            var reader = await _readerService.GetReaderAsync(id);
            if (reader == null)
            {
                return NotFound($"Reader with ID {id} not found");
            }
            
            // Update reader configuration
            reader.ReaderName = request.Name ?? reader.ReaderName;
            reader.DefaultFeedback = request.DefaultFeedback ?? reader.DefaultFeedback;
            
            if (request.ResultFeedback != null)
            {
                reader.ResultFeedback = request.ResultFeedback;
            }
            
            await _readerService.UpdateReaderAsync(reader);
            
            _logger.LogInformation("Updated reader configuration for {ReaderId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reader {ReaderId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
    
    /// <summary>
    /// Send feedback to a specific reader
    /// </summary>
    /// <param name="id">The unique identifier of the reader</param>
    /// <param name="feedback">The feedback to send (LED color, beeps, display message)</param>
    /// <returns>Success status</returns>
    /// <response code="200">The feedback was successfully sent</response>
    /// <response code="400">If the feedback could not be sent</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost("{id}/feedback")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SendFeedback(Guid id, [FromBody] ReaderFeedback feedback)
    {
        try
        {
            var success = await _readerService.SendFeedbackAsync(id, feedback);
            
            if (success)
            {
                _logger.LogInformation("Sent feedback to reader {ReaderId}: {FeedbackType}", id, feedback.Type);
                return Ok();
            }
            else
            {
                return BadRequest("Failed to send feedback to reader");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending feedback to reader {ReaderId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
    
    /// <summary>
    /// Get reader status
    /// </summary>
    /// <param name="id">The unique identifier of the reader</param>
    /// <returns>The current status of the reader including connection state and last activity</returns>
    /// <response code="200">Returns the reader status</response>
    /// <response code="404">If the reader was not found</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet("{id}/status")]
    [ProducesResponseType(typeof(ReaderStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ReaderStatusDto>> GetReaderStatus(Guid id)
    {
        try
        {
            var osdpDevice = await _osdpManager.GetDeviceAsync(id);
            if (osdpDevice == null)
            {
                return NotFound($"OSDP device with ID {id} not found");
            }
            
            var status = new ReaderStatusDto
            {
                Id = id,
                IsOnline = osdpDevice.IsOnline,
                LastActivity = osdpDevice.LastActivity,
                Address = osdpDevice.Address,
                Name = osdpDevice.Name
            };
            
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving status for reader {ReaderId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}

/// <summary>
/// Represents a card reader with its configuration and current status
/// </summary>
public class ReaderDto
{
    /// <summary>
    /// Unique identifier for the reader
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Display name of the reader
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Indicates whether the reader is currently connected and responding
    /// </summary>
    public bool IsOnline { get; set; }
    
    /// <summary>
    /// Timestamp of the last communication with the reader
    /// </summary>
    public DateTime? LastActivity { get; set; }
    
    /// <summary>
    /// Default feedback configuration used when no specific result feedback is defined
    /// </summary>
    public ReaderFeedbackConfiguration? DefaultFeedback { get; set; }
    
    /// <summary>
    /// Feedback configurations mapped to specific card read results
    /// </summary>
    public Dictionary<string, ReaderFeedbackConfiguration> ResultFeedback { get; set; } = new();
}

/// <summary>
/// Request model for updating reader configuration
/// </summary>
public class UpdateReaderRequest
{
    /// <summary>
    /// New display name for the reader (optional)
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Updated default feedback configuration (optional)
    /// </summary>
    public ReaderFeedbackConfiguration? DefaultFeedback { get; set; }
    
    /// <summary>
    /// Updated result-specific feedback configurations (optional)
    /// </summary>
    public Dictionary<string, ReaderFeedbackConfiguration>? ResultFeedback { get; set; }
}

/// <summary>
/// Represents the current status of an OSDP reader
/// </summary>
public class ReaderStatusDto
{
    /// <summary>
    /// Unique identifier for the reader
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Display name of the reader
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Indicates whether the reader is currently connected and responding
    /// </summary>
    public bool IsOnline { get; set; }
    
    /// <summary>
    /// Timestamp of the last communication with the reader
    /// </summary>
    public DateTime? LastActivity { get; set; }
    
    /// <summary>
    /// OSDP address of the reader (0-126)
    /// </summary>
    public byte Address { get; set; }
}