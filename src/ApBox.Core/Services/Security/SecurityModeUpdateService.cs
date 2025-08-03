using ApBox.Core.Data.Repositories;
using ApBox.Core.Models;
using Microsoft.Extensions.Logging;

namespace ApBox.Core.Services.Security;

/// <summary>
/// Service for handling security mode updates when keys are installed
/// </summary>
public class SecurityModeUpdateService : ISecurityModeUpdateService
{
    private readonly IReaderConfigurationRepository _readerRepository;
    private readonly IOsdpSecurityService _securityService;
    private readonly ILogger<SecurityModeUpdateService> _logger;

    public SecurityModeUpdateService(
        IReaderConfigurationRepository readerRepository,
        IOsdpSecurityService securityService,
        ILogger<SecurityModeUpdateService> logger)
    {
        _readerRepository = readerRepository;
        _securityService = securityService;
        _logger = logger;
    }

    public async Task<bool> UpdateSecurityModeAsync(Guid readerId, OsdpSecurityMode newSecurityMode, byte[]? secureChannelKey = null)
    {
        try
        {
            _logger.LogInformation("Updating security mode for reader {ReaderId} to {SecurityMode}", readerId, newSecurityMode);

            // Get the current reader configuration
            var reader = await _readerRepository.GetByIdAsync(readerId);
            if (reader == null)
            {
                _logger.LogWarning("Reader {ReaderId} not found for security mode update", readerId);
                return false;
            }

            // Update the security mode
            reader.SecurityMode = newSecurityMode;

            // Update the secure channel key if provided
            if (secureChannelKey != null)
            {
                reader.SecureChannelKey = secureChannelKey;
            }

            // Save the updated configuration
            await _readerRepository.UpdateAsync(reader);

            _logger.LogInformation("Successfully updated security mode for reader {ReaderId} to {SecurityMode}", readerId, newSecurityMode);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update security mode for reader {ReaderId}", readerId);
            return false;
        }
    }
}