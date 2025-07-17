using System.Collections;
using ApBox.Core.Models;
using ApBox.Core.Services;
using ApBox.Plugins;
using OSDP.Net;
using OSDP.Net.Model.CommandData;
using System.Security.Cryptography;
using System.Numerics;
using System.Text;
using OsdpLedColor = OSDP.Net.Model.CommandData.LedColor;
using ApBoxLedColor = ApBox.Core.Models.LedColor;

namespace ApBox.Core.OSDP;

public class OsdpDevice(
    OsdpDeviceConfiguration config,
    ILogger logger,
    IServiceProvider serviceProvider,
    ControlPanel controlPanel,
    Guid connectionId)
    : IOsdpDevice, IDisposable
{
    private bool _disposed;

    public Guid Id { get; } = config.Id;
    public byte Address { get; } = config.Address;
    public string Name { get; } = config.Name;
    public bool IsOnline { get; private set; }
    public DateTime LastActivity { get; private set; } = DateTime.UtcNow;
    public bool IsEnabled { get; } = config.IsEnabled;

    public event EventHandler<CardReadEvent>? CardRead;
    public event EventHandler<OsdpStatusChangedEventArgs>? StatusChanged;
    
    public Task<bool> ConnectAsync()
    {
        if (!IsEnabled || _disposed) return Task.FromResult(false);
        
        try
        {
            logger.LogInformation("Connecting OSDP device {DeviceName} with address {Address}", 
                Name, Address);
            
            // Subscribe to events before adding device
            controlPanel.RawCardDataReplyReceived += OnCardRead;
            controlPanel.ConnectionStatusChanged += OnConnectionStatusChanged;
            
            // Add device to the existing connection
            controlPanel.AddDevice(
                connectionId, 
                Address, 
                useCrc: true, 
                useSecureChannel: config.UseSecureChannel,
                secureChannelKey: config.SecureChannelKey);

            // Connection success is now handled by the OnConnectionStatusChanged event
            // The event will fire when the device comes online or goes offline
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to OSDP device {DeviceName}", Name);
            return Task.FromResult(false);
        }
    }
    
    public async Task DisconnectAsync()
    {
        logger.LogInformation("Disconnecting OSDP device {DeviceName}", Name);
        
        try
        {
            // Remove device from the shared connection
            controlPanel.RemoveDevice(connectionId, Address);
            await Task.Delay(500); // Give it time to shutdown gracefully
            
            // Unsubscribe from events after disconnecting
            // This ensures any final status change events are processed
            controlPanel.RawCardDataReplyReceived -= OnCardRead;
            controlPanel.ConnectionStatusChanged -= OnConnectionStatusChanged;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error during OSDP device {DeviceName} shutdown", Name);
        }
    }
    
    
    private async Task AttemptSecureChannelInstallation()
    {
        if (!IsOnline)
        {
            logger.LogWarning("Cannot install secure channel for device {DeviceName} - device not online", Name);
            return;
        }
        
        try
        {
            logger.LogInformation("Starting secure channel installation for device {DeviceName}", Name);
            
            // Generate a random 16-byte security key
            var secureChannelKey = GenerateSecureChannelKey();
            
            // Install the secure channel key using the default OSDP key
            // The OSDP.Net library should handle the key installation process
            var installSuccess = await InstallSecureChannelKey(secureChannelKey);
            
            if (installSuccess)
            {
                logger.LogInformation("Successfully installed secure channel key for device {DeviceName}", Name);
                
                // Update the configuration to use the new key
                config.SecureChannelKey = secureChannelKey;
                config.UseSecureChannel = true;
                
                // Notify that the security mode has changed
                await NotifySecurityModeChanged(OsdpSecurityMode.Secure);
            }
            else
            {
                logger.LogWarning("Failed to install secure channel key for device {DeviceName}", Name);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during secure channel installation for device {DeviceName}", Name);
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
        
        logger.LogDebug("Generated secure channel key for device {DeviceName}", Name);
        return key;
    }
    
    private async Task<bool> InstallSecureChannelKey(byte[] secureChannelKey)
    {
        // ControlPanel is always available as it's injected
        
        try
        {
            logger.LogInformation("Installing secure channel key for device {DeviceName}", Name);
            
            // Create the encryption key configuration
            // Using SecureChannelBaseKey as the key type for secure channel installation
            var keyConfiguration = new EncryptionKeyConfiguration(KeyType.SecureChannelBaseKey, secureChannelKey);
            
            // Use the OSDP.Net API to set the encryption key
            // This will install the new secure channel key on the device
            var result = await controlPanel.EncryptionKeySet(connectionId, Address, keyConfiguration);
            
            if (result)
            {
                logger.LogInformation("Successfully installed secure channel key for device {DeviceName}", Name);
                return true;
            }
            else
            {
                logger.LogWarning("Failed to install secure channel key for device {DeviceName} - device rejected the key", Name);
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to install secure channel key for device {DeviceName}", Name);
            return false;
        }
    }
    
    private async Task NotifySecurityModeChanged(OsdpSecurityMode newMode)
    {
        try
        {
            logger.LogInformation("Security mode changed to {SecurityMode} for device {DeviceName}", 
                newMode, Name);
            
            // Update the database configuration using the SecurityModeUpdateService
            using var scope = serviceProvider.CreateScope();
            var securityModeUpdateService = scope.ServiceProvider.GetRequiredService<ISecurityModeUpdateService>();
            
            var updateSuccess = await securityModeUpdateService.UpdateSecurityModeAsync(
                Id, 
                newMode, 
                config.SecureChannelKey);
            
            if (updateSuccess)
            {
                logger.LogInformation("Database updated with new security mode for device {DeviceName}", Name);
            }
            else
            {
                logger.LogWarning("Failed to update database with new security mode for device {DeviceName}", Name);
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
            logger.LogError(ex, "Error notifying security mode change for device {DeviceName}", Name);
        }
    }
    
    public async Task<bool> SendCommandAsync(OsdpCommand command)
    {
        if (!IsOnline) return false;
        
        try
        {
            LastActivity = DateTime.UtcNow;
            
            logger.LogDebug("Sending OSDP command {CommandCode} to device {DeviceName}", 
                command.CommandCode, Name);
            
            // Convert our command to OSDP.Net command and send it
            var result = await SendOsdpNetCommand(command);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send command to OSDP device {DeviceName}", Name);
            return false;
        }
    }
    
    public Task<bool> SendFeedbackAsync(ReaderFeedback feedback)
    {
        if (!IsOnline) return Task.FromResult(false);
        
        try
        {
            var success = true;
            
            // Send LED command
            if (feedback.LedColor.HasValue)
            {
                // For now, skip LED commands until we fix the constructor parameters
                // TODO: Implement proper LED control once OSDP.Net API is clarified
                logger.LogDebug("LED command requested for device {DeviceName}: {Color} for {Duration}ms (not implemented)", 
                    Name, feedback.LedColor.Value, feedback.LedDurationMs ?? 1000);
            }
            
            // Send buzzer command
            if (feedback.BeepCount.HasValue && feedback.BeepCount > 0)
            {
                // For now, skip buzzer commands until we fix the constructor parameters
                // TODO: Implement proper buzzer control once OSDP.Net API is clarified
                logger.LogDebug("Buzzer command requested for device {DeviceName}: {BeepCount} beeps (not implemented)", 
                    Name, feedback.BeepCount.Value);
            }
            
            // Send text command if supported
            if (!string.IsNullOrEmpty(feedback.DisplayMessage))
            {
                // For now, skip text commands until we fix the constructor parameters
                // TODO: Implement proper text output once OSDP.Net API is clarified
                logger.LogDebug("Text command requested for device {DeviceName}: {Message} (not implemented)", 
                    Name, feedback.DisplayMessage);
            }
            
            return Task.FromResult(success);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send feedback to OSDP device {DeviceName}", Name);
            return Task.FromResult(false);
        }
    }
    
    private void OnCardRead(object? sender, ControlPanel.RawCardDataReplyEventArgs eventArgs)
    {
        try
        {
            // Only process card reads for our device address
            if (eventArgs.Address != Address) return;
            
            LastActivity = DateTime.UtcNow;

            string bitString = BuildRawBitString(eventArgs.RawCardData.Data);
            var cardNumber = ConvertWiegandToCardNumber(eventArgs.RawCardData.Data);
            var bitLength = eventArgs.RawCardData.BitCount;
            
            var cardRead = new CardReadEvent
            {
                ReaderId = Id,
                CardNumber = cardNumber,
                BitLength = bitLength,
                Timestamp = DateTime.UtcNow,
                ReaderName = Name,
                AdditionalData =
                {
                    // Add raw data to additional data for debugging/analysis
                    ["ReaderFormat"] = eventArgs.RawCardData.FormatCode.ToString() ?? "Unknown",
                    ["BitString"] = bitString
                }
            };

            logger.LogInformation("Card read on OSDP device {DeviceName}: {CardNumber} ({BitLength} bits) - Raw: {RawData}", 
                Name, cardNumber, bitLength, bitString);
            
            CardRead?.Invoke(this, cardRead);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing card read from OSDP device {DeviceName}", Name);
        }
    }
    
    private void OnConnectionStatusChanged(object? sender, EventArgs args)
    {
        // For now, use a simpler approach since we can't access the exact event args
        // In a real implementation, we would extract connection status from the event args
        var wasOnline = IsOnline; 
        
        // Check the device online status using the control panel
        IsOnline = controlPanel.IsOnline(connectionId, Address);
        
        if (wasOnline != IsOnline)
        {
            if (IsOnline)
            {
                logger.LogInformation("OSDP device {DeviceName} connected successfully on address {Address}", 
                    Name, Address);
                
                StatusChanged?.Invoke(this, new OsdpStatusChangedEventArgs
                {
                    IsOnline = true,
                    Message = "Connected successfully"
                });
                
                // If in Install Mode, attempt to install secure channel key
                if (config.SecurityMode == OsdpSecurityMode.Install)
                {
                    _ = Task.Run(async () => await AttemptSecureChannelInstallation());
                }
            }
            else
            {
                logger.LogInformation("OSDP device {DeviceName} went offline", Name);
                
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
                logger.LogDebug("LED command requested for device {DeviceName} (not implemented)", Name);
                return false;
                
            case BuzzerCommand buzzerCmd:
                // For now, skip buzzer commands until we fix the constructor parameters
                // TODO: Implement proper buzzer control once OSDP.Net API is clarified
                logger.LogDebug("Buzzer command requested for device {DeviceName} (not implemented)", Name);
                return false;
                
            default:
                return false;
        }
    }
    
    /// <summary>
    /// Converts a BitArray to a binary string representation (as per Aporta WiegandCredentialHandler)
    /// </summary>
    private static string BuildRawBitString(BitArray cardData)
    {
        var cardNumberBuilder = new StringBuilder();
        foreach (bool bit in cardData)
        {
            cardNumberBuilder.Append(bit ? "1" : "0");
        }
        return cardNumberBuilder.ToString();
    }
    
    /// <summary>
    /// Converts Wiegand card data to a decimal card number
    /// Based on Aporta implementation but enhanced for larger numbers
    /// </summary>
    private string ConvertWiegandToCardNumber(BitArray cardData)
    {
        if (cardData.Length == 0) return "0";
        
        // Convert bit array to binary string for processing
        var bitString = BuildRawBitString(cardData);
        
        // For standard Wiegand formats, we typically need to extract the card number portion
        // Most common is Wiegand 26-bit: 1 bit parity + 8 bits facility + 16 bits card number + 1 bit parity
        // But we'll support variable length by converting the entire bit string to decimal
        
        try
        {
            // Convert binary string to decimal using BigInteger for large number support
            if (bitString.All(c => c == '0'))
            {
                return "0";
            }
            
            var cardNumber = BigInteger.Zero;
            var powerOfTwo = BigInteger.One;
            
            // Process bits from right to left (least significant to most significant)
            for (int i = bitString.Length - 1; i >= 0; i--)
            {
                if (bitString[i] == '1')
                {
                    cardNumber += powerOfTwo;
                }
                powerOfTwo *= 2;
            }
            
            return cardNumber.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error converting bit string to card number: {BitString}", bitString);
            return "0";
        }
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
            logger.LogWarning(ex, "Error during OSDP device {DeviceName} disposal", Name);
        }
        
        // Unsubscribe from events
        try
        {
            controlPanel.RawCardDataReplyReceived -= OnCardRead;
            controlPanel.ConnectionStatusChanged -= OnConnectionStatusChanged;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error unsubscribing from events during disposal for device {DeviceName}", Name);
        }
        
        // ControlPanel is shared, so we don't dispose it here
        _disposed = true;
    }
}