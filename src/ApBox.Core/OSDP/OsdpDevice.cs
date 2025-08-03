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
    ControlPanel controlPanel,
    Guid connectionId,
    IFeedbackConfigurationService feedbackConfigurationService)
    : IOsdpDevice, IDisposable
{
    private bool _disposed;
    private Timer? _idleTimer;

    public Guid Id { get; } = config.Id;
    public byte Address { get; } = config.Address;
    public string Name { get; } = config.Name;
    public bool IsOnline { get; private set; }
    public DateTime LastActivity { get; private set; } = DateTime.UtcNow;
    public bool IsEnabled { get; } = config.IsEnabled;

    public event EventHandler<CardReadEvent>? CardRead;
    public event EventHandler<PinDigitEvent>? PinDigitReceived;
    public event EventHandler<OsdpStatusChangedEventArgs>? StatusChanged;
    public event EventHandler<SecurityModeChangedEventArgs>? SecurityModeChanged;
    
    public Task<bool> ConnectAsync()
    {
        if (!IsEnabled || _disposed) return Task.FromResult(false);
        
        try
        {
            logger.LogInformation("Connecting OSDP device {DeviceName} with address {Address}", 
                Name, Address);
            
            // Subscribe to events before adding device
            controlPanel.RawCardDataReplyReceived += OnCardRead;
            controlPanel.KeypadReplyReceived += OnKeypadReply;
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
            StopIdleTimer();
                
            // Remove device from the shared connection
            controlPanel.RemoveDevice(connectionId, Address);
            await Task.Delay(500); // Give it time to shutdown gracefully
            
            // Unsubscribe from events after disconnecting
            // This ensures any final status change events are processed
            controlPanel.RawCardDataReplyReceived -= OnCardRead;
            controlPanel.KeypadReplyReceived -= OnKeypadReply;
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
    
    private Task NotifySecurityModeChanged(OsdpSecurityMode newMode)
    {
        try
        {
            logger.LogInformation("Security mode changed to {SecurityMode} for device {DeviceName}", 
                newMode, Name);
            
            // Fire security mode changed event for external services to handle database updates
            SecurityModeChanged?.Invoke(this, new SecurityModeChangedEventArgs
            {
                DeviceId = Id,
                NewMode = newMode,
                SecureChannelKey = config.SecureChannelKey,
                DeviceName = Name,
                Timestamp = DateTime.UtcNow
            });
            
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

        return Task.CompletedTask;
    }
    
    
    public async Task<bool> SendFeedbackAsync(ReaderFeedback feedback)
    {
        if (!IsOnline) return false;
        
        try
        {
            LastActivity = DateTime.UtcNow;
            var success = true;
            
            // Pause idle timer during feedback for the LED duration
            if (feedback.LedColor.HasValue && feedback.LedDuration > 0)
            {
                PauseIdleTimerForFeedback(feedback.LedDuration);
            }
            
            // Send LED command if LED color is specified
            if (feedback.LedColor.HasValue)
            {
                var osdpLedColor = ConvertLedColor(feedback.LedColor.Value);
                
                // Create LED control using OSDP.Net directly
                var readerLedControl = new ReaderLedControl(
                    readerNumber: 0, // Default reader number for single-reader devices
                    ledNumber: 0,    // First LED
                    temporaryMode: TemporaryReaderControlCode.SetTemporaryAndStartTimer,
                    temporaryOnTime: 5, // 500ms (5 * 100ms units)
                    temporaryOffTime: 5, // 500ms (5 * 100ms units)
                    temporaryOnColor: osdpLedColor,
                    temporaryOffColor: OsdpLedColor.Black,
                    temporaryTimer: (ushort)(feedback.LedDuration * 10), // Convert seconds to 100ms units
                    permanentMode: PermanentReaderControlCode.Nop,
                    permanentOnTime: 1,
                    permanentOffTime: 1,
                    permanentOnColor: OsdpLedColor.Black,
                    permanentOffColor: OsdpLedColor.Black // Keep LED solid in idle state
                );
                
                var readerLedControls = new ReaderLedControls([readerLedControl]);
                var ledResult = await controlPanel.ReaderLedControl(connectionId, Address, readerLedControls);
                success = success && ledResult;
                
                logger.LogDebug("LED feedback sent to device {DeviceName}: {Color} for {Duration}s - Result: {Result}", 
                    Name, feedback.LedColor.Value, feedback.LedDuration, ledResult);
            }
            
            // Send buzzer command if beep count is specified
            if (feedback.BeepCount > 0)
            {
                // Create buzzer control using OSDP.Net directly
                var readerBuzzerControl = new ReaderBuzzerControl(
                    readerNumber: 0, // Default reader number for single-reader devices
                    toneCode: ToneCode.Default,
                    onTime: 2,  // 200ms beep
                    offTime: 2, // 200ms pause
                    count: (byte)feedback.BeepCount
                );
                
                var buzzerResult = await controlPanel.ReaderBuzzerControl(connectionId, Address, readerBuzzerControl);
                success = success && buzzerResult;
                
                logger.LogDebug("Buzzer feedback sent to device {DeviceName}: {BeepCount} beeps - Result: {Result}", 
                    Name, feedback.BeepCount, buzzerResult);
            }
            
            // Send text command if supported (not yet implemented)
            if (!string.IsNullOrEmpty(feedback.DisplayMessage))
            {
                // Text display commands are not yet implemented in OSDP.Net or our command structure
                // This would require implementing TextOutputCommand and corresponding OSDP.Net API calls
                logger.LogDebug("Text command requested for device {DeviceName}: {Message} (text display not yet implemented)", 
                    Name, feedback.DisplayMessage);
                // Don't mark as failure since text is optional
            }
            
            return success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send feedback to OSDP device {DeviceName}", Name);
            return false;
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
                    ["ReaderFormat"] = eventArgs.RawCardData.FormatCode.ToString(),
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

    private void OnKeypadReply(object? sender, ControlPanel.KeypadReplyEventArgs eventArgs)
    {
        try
        {
            // Only process keypad events for our device address
            if (eventArgs.Address != Address) return;
            
            LastActivity = DateTime.UtcNow;

            // Convert keypad data to individual digits
            var keypadData = eventArgs.KeypadData;
            
            logger.LogDebug("Keypad data received on OSDP device {DeviceName}: {KeypadData}", 
                Name, Convert.ToHexString(keypadData.Data));

            // Process each digit in the keypad data
            for (int i = 0; i < keypadData.DigitCount; i++)
            {
                if (i < keypadData.Data.Length)
                {
                    var rawByte = keypadData.Data[i];
                    
                    // Map OSDP keypad codes to standard characters
                    var digit = rawByte switch
                    {
                        0x0D => '#',  // OSDP pound key sends 0x0D (carriage return)
                        0x7F => '*',  // OSDP asterisk key sends 0x7F (DEL character)
                        _ when rawByte >= 0x30 && rawByte <= 0x39 => (char)rawByte, // Standard digits 0-9
                        _ => (char)rawByte  // Pass through other characters as-is
                    };
                    
                    var pinDigitEvent = new PinDigitEvent
                    {
                        ReaderId = Id,
                        Digit = digit,
                        Timestamp = DateTime.UtcNow,
                        ReaderName = Name,
                        SequenceNumber = i + 1
                    };

                    logger.LogDebug("PIN digit received on OSDP device {DeviceName}: raw=0x{RawByte:X2} mapped='{Digit}'", 
                        Name, rawByte, digit);
                    PinDigitReceived?.Invoke(this, pinDigitEvent);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing keypad data from OSDP device {DeviceName}", Name);
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
                
                // Start idle timer for heartbeat flashing
                StartIdleTimer();
            }
            else
            {
                logger.LogInformation("OSDP device {DeviceName} went offline", Name);
                
                // Stop idle timer when device goes offline
                StopIdleTimer();
                
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
    
    private void StartIdleTimer()
    {
        try
        {
            // Stop existing timer if running
            _idleTimer?.Dispose();
            
            // Start timer immediately and then every 5 seconds for heartbeat flash
            _idleTimer = new Timer(OnIdleHeartbeat, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            
            logger.LogDebug("Started idle heartbeat timer for device {DeviceName}", Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start idle timer for device {DeviceName}", Name);
        }
    }
    
    private void StopIdleTimer()
    {
        try
        {
            _idleTimer?.Dispose();
            _idleTimer = null;
            
            logger.LogDebug("Stopped idle heartbeat timer for device {DeviceName}", Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error stopping idle timer for device {DeviceName}", Name);
        }
    }
    
    private void OnIdleHeartbeat(object? state)
    {
        if (!IsOnline || _disposed) return;
        
        // Fire and forget with proper error handling
        _ = Task.Run(async () =>
        {
            try
            {
                await SetIdleLedStateAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in idle heartbeat for device {DeviceName}", Name);
            }
        });
    }
    
    private async Task SetIdleLedStateAsync()
    {
        try
        {
            // Get idle state configuration from database with defaults as fallback
            var permanentColor = OsdpLedColor.Black;
            var heartbeatColor = OsdpLedColor.Black;
            
            try
            {
                var idleState = await feedbackConfigurationService.GetIdleStateAsync();
                
                if (idleState.PermanentLedColor.HasValue)
                {
                    permanentColor = ConvertLedColor(idleState.PermanentLedColor.Value);
                }
                
                if (idleState.HeartbeatFlashColor.HasValue)
                {
                    heartbeatColor = ConvertLedColor(idleState.HeartbeatFlashColor.Value);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to get idle state configuration from database for device {DeviceName}, using defaults", Name);
            }
            
            // Create LED control for heartbeat flash
            var readerLedControl = new ReaderLedControl(
                readerNumber: 0,
                ledNumber: 0,
                temporaryMode: TemporaryReaderControlCode.SetTemporaryAndStartTimer,
                temporaryOnTime: 2, // 500ms flash (5 * 100ms units)
                temporaryOffTime: 2, // 500ms off (5 * 100ms units)
                temporaryOnColor: heartbeatColor,
                temporaryOffColor: permanentColor,
                temporaryTimer: 4, // Flash for 1 second (10 * 100ms units)
                permanentMode: PermanentReaderControlCode.SetPermanentState,
                permanentOnTime: 1,
                permanentOffTime: 1,
                permanentOnColor: permanentColor,
                permanentOffColor: permanentColor // Return to permanent color
            );
            
            var readerLedControls = new ReaderLedControls([readerLedControl]);
            var result = await controlPanel.ReaderLedControl(connectionId, Address, readerLedControls);
            
            if (result)
            {
                logger.LogDebug("Idle heartbeat flash sent to device {DeviceName}: Green on Blue", Name);
            }
            else
            {
                logger.LogWarning("Failed to send idle heartbeat flash to device {DeviceName}", Name);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting idle LED state for device {DeviceName}", Name);
        }
    }
    
    private void PauseIdleTimerForFeedback(int feedbackDurationSeconds)
    {
        try
        {
            // Stop the idle timer during feedback
            StopIdleTimer();
            
            // Schedule restart after feedback duration
            _ = Task.Delay(TimeSpan.FromSeconds(feedbackDurationSeconds)).ContinueWith(_ => 
            {
                if (IsOnline && !_disposed)
                {
                    StartIdleTimer();
                }
            });
            
            logger.LogDebug("Paused idle timer for {Duration}s during feedback on device {DeviceName}", 
                feedbackDurationSeconds, Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error pausing idle timer for device {DeviceName}", Name);
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
            controlPanel.KeypadReplyReceived -= OnKeypadReply;
            controlPanel.ConnectionStatusChanged -= OnConnectionStatusChanged;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error unsubscribing from events during disposal for device {DeviceName}", Name);
        }
        
        // Dispose idle timer
        try
        {
            _idleTimer?.Dispose();
            _idleTimer = null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error disposing idle timer during disposal for device {DeviceName}", Name);
        }
        
        // ControlPanel is shared, so we don't dispose it here
        _disposed = true;
    }
}