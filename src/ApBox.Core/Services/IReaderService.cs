using ApBox.Core.Models;
using ApBox.Plugins;

namespace ApBox.Core.Services;

public interface IReaderService
{
    Task<IEnumerable<ReaderConfiguration>> GetReadersAsync();
    Task<ReaderConfiguration?> GetReaderAsync(Guid readerId);
    Task UpdateReaderAsync(ReaderConfiguration reader);
    Task<bool> SendFeedbackAsync(Guid readerId, ReaderFeedback feedback);
}

public class ReaderService : IReaderService
{
    private readonly IReaderConfigurationService _configurationService;
    private readonly ILogger<ReaderService> _logger;
    
    public ReaderService(
        IReaderConfigurationService configurationService,
        ILogger<ReaderService> logger)
    {
        _configurationService = configurationService;
        _logger = logger;
    }
    
    public async Task<IEnumerable<ReaderConfiguration>> GetReadersAsync()
    {
        return await _configurationService.GetAllReadersAsync();
    }
    
    public async Task<ReaderConfiguration?> GetReaderAsync(Guid readerId)
    {
        return await _configurationService.GetReaderAsync(readerId);
    }
    
    public async Task UpdateReaderAsync(ReaderConfiguration reader)
    {
        await _configurationService.SaveReaderAsync(reader);
    }
    
    public async Task<bool> SendFeedbackAsync(Guid readerId, ReaderFeedback feedback)
    {
        try
        {
            _logger.LogInformation("Sending feedback to reader {ReaderId}: {FeedbackType}", 
                readerId, feedback.Type);
            
            // TODO: Implement actual OSDP communication
            // For now, just log the feedback
            _logger.LogDebug("Feedback details - Beeps: {BeepCount}, LED: {LedColor}, Duration: {Duration}ms", 
                feedback.BeepCount, feedback.LedColor, feedback.LedDurationMs);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send feedback to reader {ReaderId}", readerId);
            return false;
        }
    }
}