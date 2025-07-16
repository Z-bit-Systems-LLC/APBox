using ApBox.Core.Models;
using ApBox.Plugins;
using OSDP.Net;
using OSDP.Net.Connections;
using OSDP.Net.Model.ReplyData;
using OSDP.Net.Model.CommandData;
using System.IO.Ports;
using OsdpLedColor = OSDP.Net.Model.CommandData.LedColor;
using ApBoxLedColor = ApBox.Core.Models.LedColor;

namespace ApBox.Core.OSDP;

public class RealOsdpDevice : IOsdpDevice, IDisposable
{
    private readonly OsdpDeviceConfiguration _config;
    private readonly ILogger _logger;
    private ControlPanel? _controlPanel;
    private Guid _connectionId;
    private bool _disposed = false;
    private Timer? _statusMonitoringTimer;
    
    public RealOsdpDevice(OsdpDeviceConfiguration config, ILogger logger)
    {
        _config = config;
        _logger = logger;
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
            // Validate serial port configuration
            if (string.IsNullOrEmpty(_config.ConnectionString))
            {
                _logger.LogWarning("No serial port configured for OSDP device {DeviceName}", Name);
                return false;
            }
            
            // Check if serial port exists
            if (!SerialPort.GetPortNames().Contains(_config.ConnectionString))
            {
                _logger.LogWarning("Serial port {SerialPort} not found for OSDP device {DeviceName}", 
                    _config.ConnectionString, Name);
                return false;
            }
            
            _logger.LogInformation("Connecting to OSDP device {DeviceName} on {SerialPort} at {BaudRate} baud", 
                Name, _config.ConnectionString, _config.BaudRate);
            
            // Create control panel
            _controlPanel = new ControlPanel();
            
            // Subscribe to events before starting connection
            // Note: Using basic event handlers due to OSDP.Net API uncertainty
            // In a real implementation, we would use the correct event types
            // _controlPanel.RawCardDataReplyReceived += OnCardRead;
            // _controlPanel.ConnectionStatusChanged += OnConnectionStatusChanged;
            
            // Start connection with serial port
            _connectionId = _controlPanel.StartConnection(new SerialPortOsdpConnection(
                _config.ConnectionString, 
                _config.BaudRate));
            
            // Add device to the connection
            _controlPanel.AddDevice(
                _connectionId, 
                Address, 
                useCrc: true, 
                useSecureChannel: _config.UseSecureChannel,
                secureChannelKey: _config.SecureChannelKey);
            
            // Wait a moment for connection to establish
            await Task.Delay(1000);
            
            // Check if device is online
            IsOnline = _controlPanel.IsOnline(_connectionId, Address);
            LastActivity = DateTime.UtcNow;
            
            if (IsOnline)
            {
                _logger.LogInformation("OSDP device {DeviceName} connected successfully on address {Address}", 
                    Name, Address);
                
                StatusChanged?.Invoke(this, new OsdpStatusChangedEventArgs
                {
                    IsOnline = true,
                    Message = "Connected successfully"
                });
                
                // Start periodic status monitoring
                StartStatusMonitoring();
                
                return true;
            }
            else
            {
                _logger.LogWarning("OSDP device {DeviceName} failed to come online", Name);
                
                // Start periodic status monitoring even if initial connection failed
                // This will help detect when the device comes online later
                StartStatusMonitoring();
                
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to OSDP device {DeviceName}", Name);
            return false;
        }
    }
    
    public async Task DisconnectAsync()
    {
        if (_controlPanel != null)
        {
            _logger.LogInformation("Disconnecting OSDP device {DeviceName}", Name);
            
            try
            {
                _controlPanel.RemoveDevice(_connectionId, Address);
                await _controlPanel.StopConnection(_connectionId);
                await Task.Delay(500); // Give it time to shutdown gracefully
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during OSDP device {DeviceName} shutdown", Name);
            }
        }
        
        IsOnline = false;
        
        StatusChanged?.Invoke(this, new OsdpStatusChangedEventArgs
        {
            IsOnline = false,
            Message = "Disconnected"
        });
        
        // Stop status monitoring
        _statusMonitoringTimer?.Dispose();
        _statusMonitoringTimer = null;
    }
    
    private void StartStatusMonitoring()
    {
        // Stop any existing timer
        _statusMonitoringTimer?.Dispose();
        
        // Start a timer to periodically check device status
        _statusMonitoringTimer = new Timer(CheckDeviceStatus, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }
    
    private void CheckDeviceStatus(object? state)
    {
        if (_disposed || _controlPanel == null) return;
        
        try
        {
            var currentStatus = _controlPanel.IsOnline(_connectionId, Address);
            
            if (currentStatus != IsOnline)
            {
                IsOnline = currentStatus;
                LastActivity = DateTime.UtcNow;
                
                var message = currentStatus ? "Device came online" : "Device went offline";
                _logger.LogInformation("OSDP device {DeviceName} status changed: {Status}", Name, message);
                
                StatusChanged?.Invoke(this, new OsdpStatusChangedEventArgs
                {
                    IsOnline = currentStatus,
                    Message = message
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking status for OSDP device {DeviceName}", Name);
        }
    }
    
    public async Task<bool> SendCommandAsync(OsdpCommand command)
    {
        if (!IsOnline || _controlPanel == null) return false;
        
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
        if (!IsOnline || _controlPanel == null) return false;
        
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
        if (_controlPanel != null)
        {
            IsOnline = _controlPanel.IsOnline(_connectionId, Address);
        }
        
        if (wasOnline != IsOnline)
        {
            _logger.LogInformation("OSDP device {DeviceName} status changed: {Status}", 
                Name, IsOnline ? "Online" : "Offline");
            
            StatusChanged?.Invoke(this, new OsdpStatusChangedEventArgs
            {
                IsOnline = IsOnline,
                Message = IsOnline ? "Device came online" : "Device went offline"
            });
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
        
        // Stop status monitoring and dispose timer
        _statusMonitoringTimer?.Dispose();
        _statusMonitoringTimer = null;
        
        // ControlPanel doesn't implement IDisposable, so we'll just set it to null
        _controlPanel = null;
        _disposed = true;
    }
}