using ApBox.Core.OSDP;
using ApBox.Core.Services;
using ApBox.Plugins;
using Microsoft.AspNetCore.Mvc;

namespace ApBox.Core.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    [HttpGet]
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
    [HttpGet("{id}")]
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
    [HttpPut("{id}")]
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
    [HttpPost("{id}/feedback")]
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
    [HttpGet("{id}/status")]
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

public class ReaderDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public DateTime? LastActivity { get; set; }
    public ReaderFeedbackConfiguration? DefaultFeedback { get; set; }
    public Dictionary<string, ReaderFeedbackConfiguration> ResultFeedback { get; set; } = new();
}

public class UpdateReaderRequest
{
    public string? Name { get; set; }
    public ReaderFeedbackConfiguration? DefaultFeedback { get; set; }
    public Dictionary<string, ReaderFeedbackConfiguration>? ResultFeedback { get; set; }
}

public class ReaderStatusDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public DateTime? LastActivity { get; set; }
    public byte Address { get; set; }
}