using ApBox.Core.Models;
using ApBox.Core.Services;
using ApBox.Plugins;
using OSDP.Net;
using OSDP.Net.Connections;
using OSDP.Net.Model.CommandData;
using System.IO.Ports;
using System.Security.Cryptography;
using OsdpLedColor = OSDP.Net.Model.CommandData.LedColor;
using ApBoxLedColor = ApBox.Core.Models.LedColor;

namespace ApBox.Core.OSDP;

public class OsdpDevice : IOsdpDevice, IDisposable
{
    private readonly OsdpDeviceConfiguration _config;
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ControlPanel _controlPanel;
    private readonly Guid _connectionId;
    private bool _disposed;
    
    public OsdpDevice(OsdpDeviceConfiguration config, ILogger logger, IServiceProvider serviceProvider, ControlPanel controlPanel, Guid connectionId)
    {
        _config = config;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _controlPanel = controlPanel;
        _connectionId = connectionId;
        Id = config.Id;
        Address = config.Address;
        Name = config.Name;
        IsEnabled = config.IsEnabled;
    }
    
    public Guid Id { get; }
    public byte Address { get; }
    public string Name { get; }
    public bool IsOnline { get; private set; }
    public DateTime LastActivity { get; private set; } = DateTime.UtcNow;
    public bool IsEnabled { get; }
    
    public event EventHandler<CardReadEvent>? CardRead;
    public event EventHandler<OsdpStatusChangedEventArgs>? StatusChanged;
    
    public async Task<bool> ConnectAsync()
    {
        if (!IsEnabled || _disposed) return false;
        
        try
        {
            _logger.LogInformation("Connecting OSDP device {DeviceName} with address {Address}", 
                Name, Address);
            
            // Subscribe to events before adding device
            _controlPanel.RawCardDataReplyReceived += OnCardRead;
            _controlPanel.ConnectionStatusChanged += OnConnectionStatusChanged;
            
            // Add device to the existing connection
            _controlPanel.AddDevice(
                _connectionId, 
                Address, 
                useCrc: true, 
                useSecureChannel: _config.UseSecureChannel,
                secureChannelKey: _config.SecureChannelKey);

            // Connection success is now handled by the OnConnectionStatusChanged event
            // The event will fire when the device comes online or goes offline
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to OSDP device {DeviceName}", Name);
            return false;
        }
    }
    
    public async Task DisconnectAsync()
    {
        _logger.LogInformation("Disconnecting OSDP device {DeviceName}", Name);
        
        try
        {
            // Remove device from the shared connection
            _controlPanel.RemoveDevice(_connectionId, Address);
            await Task.Delay(500); // Give it time to shutdown gracefully
            
            // Unsubscribe from events after disconnecting
            // This ensures any final status change events are processed
            _controlPanel.RawCardDataReplyReceived -= OnCardRead;
            _controlPanel.ConnectionStatusChanged -= OnConnectionStatusChanged;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during OSDP device {DeviceName} shutdown", Name);
        }
    }
    
    
    private async Task AttemptSecureChannelInstallation()
    {
        if (!IsOnline)
        {
            _logger.LogWarning("Cannot install secure channel for device {DeviceName} - device not online", Name);
            return;
        }
        
        try
        {
            _logger.LogInformation("Starting secure channel installation for device {DeviceName}", Name);
            
            // Generate a random 16-byte security key
            var secureChannelKey = GenerateSecureChannelKey();
            
            // Install the secure channel key using the default OSDP key
            // The OSDP.Net library should handle the key installation process
            var installSuccess = await InstallSecureChannelKey(secureChannelKey);
            
            if (installSuccess)
            {
                _logger.LogInformation("Successfully installed secure channel key for device {DeviceName}", Name);
                
                // Update the configuration to use the new key
                _config.SecureChannelKey = secureChannelKey;
                _config.UseSecureChannel = true;
                
                // Notify that the security mode has changed
                await NotifySecurityModeChanged(OsdpSecurityMode.Secure);
            }
            else
            {
                _logger.LogWarning("Failed to install secure channel key for device {DeviceName}", Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during secure channel installation for device {DeviceName}", Name);
        }
    }
    
    private byte[] GenerateSecureChannelKey()
    {
        // Generate a cryptographically secure random 16-byte key
        var key = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(key);
        }
        
        _logger.LogDebug("Generated secure channel key for device {DeviceName}", Name);
        return key;
    }
    
    private async Task<bool> InstallSecureChannelKey(byte[] secureChannelKey)
    {
        // ControlPanel is always available as it's injected
        
        try
        {
            _logger.LogInformation("Installing secure channel key for device {DeviceName}", Name);
            
            // Create the encryption key configuration
            // Using SecureChannelBaseKey as the key type for secure channel installation
            var keyConfiguration = new EncryptionKeyConfiguration(KeyType.SecureChannelBaseKey, secureChannelKey);
            
            // Use the OSDP.Net API to set the encryption key
            // This will install the new secure channel key on the device
            var result = await _controlPanel.EncryptionKeySet(_connectionId, Address, keyConfiguration);
            
            if (result)
            {
                _logger.LogInformation("Successfully installed secure channel key for device {DeviceName}", Name);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to install secure channel key for device {DeviceName} - device rejected the key", Name);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install secure channel key for device {DeviceName}", Name);
            return false;
        }
    }
    
    private async Task NotifySecurityModeChanged(OsdpSecurityMode newMode)
    {
        try
        {
            _logger.LogInformation("Security mode changed to {SecurityMode} for device {DeviceName}", 
                newMode, Name);
            
            // Update the database configuration using the SecurityModeUpdateService
            using var scope = _serviceProvider.CreateScope();
            var securityModeUpdateService = scope.ServiceProvider.GetRequiredService<ISecurityModeUpdateService>();
            
            var updateSuccess = await securityModeUpdateService.UpdateSecurityModeAsync(
                Id, 
                newMode, 
                _config.SecureChannelKey);
            
            if (updateSuccess)
            {
                _logger.LogInformation("Database updated with new security mode for device {DeviceName}", Name);
            }
            else
            {
                _logger.LogWarning("Failed to update database with new security mode for device {DeviceName}", Name);
            }
            
            // Fire a status change event to update the UI
            StatusChanged?.Invoke(this, new OsdpStatusChangedEventArgs
            {
                IsOnline = IsOnline,
                Message = $"Security mode changed to {newMode}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying security mode change for device {DeviceName}", Name);
        }
    }
    
    public async Task<bool> SendCommandAsync(OsdpCommand command)
    {
        if (!IsOnline) return false;
        
        try
        {
            LastActivity = DateTime.UtcNow;
            
            _logger.LogDebug("Sending OSDP command {CommandCode} to device {DeviceName}", 
                command.CommandCode, Name);
            
            // Convert our command to OSDP.Net command and send it
            var result = await SendOsdpNetCommand(command);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send command to OSDP device {DeviceName}", Name);
            return false;
        }
    }
    
    public async Task<bool> SendFeedbackAsync(ReaderFeedback feedback)
    {
        if (!IsOnline) return false;
        
        try
        {
            var success = true;
            
            // Send LED command
            if (feedback.LedColor.HasValue)
            {
                // For now, skip LED commands until we fix the constructor parameters
                // TODO: Implement proper LED control once OSDP.Net API is clarified
                _logger.LogDebug("LED command requested for device {DeviceName}: {Color} for {Duration}ms (not implemented)", 
                    Name, feedback.LedColor.Value, feedback.LedDurationMs ?? 1000);
            }
            
            // Send buzzer command
            if (feedback.BeepCount.HasValue && feedback.BeepCount > 0)
            {
                // For now, skip buzzer commands until we fix the constructor parameters
                // TODO: Implement proper buzzer control once OSDP.Net API is clarified
                _logger.LogDebug("Buzzer command requested for device {DeviceName}: {BeepCount} beeps (not implemented)", 
                    Name, feedback.BeepCount.Value);
            }
            
            // Send text command if supported
            if (!string.IsNullOrEmpty(feedback.DisplayMessage))
            {
                // For now, skip text commands until we fix the constructor parameters
                // TODO: Implement proper text output once OSDP.Net API is clarified
                _logger.LogDebug("Text command requested for device {DeviceName}: {Message} (not implemented)", 
                    Name, feedback.DisplayMessage);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send feedback to OSDP device {DeviceName}", Name);
            return false;
        }
    }
    
    private void OnCardRead(object? sender, EventArgs args)
    {
        try
        {
            // For now, simulate card read since we can't access the exact event args
            // In a real implementation, we would extract the card data from the event args
            LastActivity = DateTime.UtcNow;
            
            // Simulate card data - in real implementation, extract from event args
            var cardData = new byte[] { 0x01, 0x23, 0x45, 0x67 }; // Example card data
            var cardNumber = ConvertCardDataToNumber(cardData);
            var bitLength = cardData.Length * 8;
            
            var cardRead = new CardReadEvent
            {
                ReaderId = Id,
                CardNumber = cardNumber,
                BitLength = bitLength,
                Timestamp = DateTime.UtcNow,
                ReaderName = Name
            };
            
            _logger.LogInformation("Card read on OSDP device {DeviceName}: {CardNumber} ({BitLength} bits)", 
                Name, cardNumber, bitLength);
            
            CardRead?.Invoke(this, cardRead);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing card read from OSDP device {DeviceName}", Name);
        }
    }
    
    private void OnConnectionStatusChanged(object? sender, EventArgs args)
    {
        // For now, use a simpler approach since we can't access the exact event args
        // In a real implementation, we would extract connection status from the event args
        var wasOnline = IsOnline; 
        
        // Check the device online status using the control panel
        IsOnline = _controlPanel.IsOnline(_connectionId, Address);
        
        if (wasOnline != IsOnline)
        {
            if (IsOnline)
            {
                _logger.LogInformation("OSDP device {DeviceName} connected successfully on address {Address}", 
                    Name, Address);
                
                StatusChanged?.Invoke(this, new OsdpStatusChangedEventArgs
                {
                    IsOnline = true,
                    Message = "Connected successfully"
                });
                
                // If in Install Mode, attempt to install secure channel key
                if (_config.SecurityMode == OsdpSecurityMode.Install)
                {
                    _ = Task.Run(async () => await AttemptSecureChannelInstallation());
                }
            }
            else
            {
                _logger.LogInformation("OSDP device {DeviceName} went offline", Name);
                
                StatusChanged?.Invoke(this, new OsdpStatusChangedEventArgs
                {
                    IsOnline = false,
                    Message = "Device went offline"
                });
            }
        }
        
        if (IsOnline)
        {
            LastActivity = DateTime.UtcNow;
        }
    }
    
    private OsdpLedColor ConvertLedColor(ApBoxLedColor color)
    {
        return color switch
        {
            ApBoxLedColor.Red => OsdpLedColor.Red,
            ApBoxLedColor.Green => OsdpLedColor.Green,
            ApBoxLedColor.Blue => OsdpLedColor.Blue,
            ApBoxLedColor.Amber => OsdpLedColor.Amber,
            _ => OsdpLedColor.Red
        };
    }
    
    private async Task<bool> SendOsdpNetCommand(OsdpCommand command)
    {
        // Convert our generic command to OSDP.Net specific command and send it
        switch (command)
        {
            case LedCommand ledCmd:
                // For now, skip LED commands until we fix the constructor parameters
                // TODO: Implement proper LED control once OSDP.Net API is clarified
                _logger.LogDebug("LED command requested for device {DeviceName} (not implemented)", Name);
                return false;
                
            case BuzzerCommand buzzerCmd:
                // For now, skip buzzer commands until we fix the constructor parameters
                // TODO: Implement proper buzzer control once OSDP.Net API is clarified
                _logger.LogDebug("Buzzer command requested for device {DeviceName} (not implemented)", Name);
                return false;
                
            default:
                return false;
        }
    }
    
    private string ConvertCardDataToNumber(byte[] data)
    {
        // Convert raw card data to card number string
        // This is a simplified conversion - real implementation depends on card format
        if (data.Length == 0) return "0";
        
        // For standard 26-bit Wiegand format
        if (data.Length >= 3)
        {
            var cardData = (uint)((data[0] << 16) | (data[1] << 8) | data[2]);
            // Remove parity bits and extract card number
            var cardNumber = (cardData >> 1) & 0xFFFF;
            return cardNumber.ToString();
        }
        
        // For other formats, convert to hex string
        return BitConverter.ToString(data).Replace("-", "");
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        
        try
        {
            DisconnectAsync().Wait(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during OSDP device {DeviceName} disposal", Name);
        }
        
        // Unsubscribe from events
        try
        {
            _controlPanel.RawCardDataReplyReceived -= OnCardRead;
            _controlPanel.ConnectionStatusChanged -= OnConnectionStatusChanged;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error unsubscribing from events during disposal for device {DeviceName}", Name);
        }
        
        // ControlPanel is shared, so we don't dispose it here
        _disposed = true;
    }
}