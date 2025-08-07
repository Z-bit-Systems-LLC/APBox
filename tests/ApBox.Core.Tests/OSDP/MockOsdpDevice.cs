using ApBox.Core.Models;
using ApBox.Core.OSDP;
using ApBox.Plugins;
using Microsoft.Extensions.Logging;

namespace ApBox.Core.Tests.OSDP;

public class MockOsdpDevice : IOsdpDevice
{
    private readonly ILogger _logger;
    private readonly Timer? _simulationTimer;
    private readonly Random _random = new();
    
    public MockOsdpDevice(OsdpDeviceConfiguration config, ILogger logger)
    {
        _logger = logger;
        Id = config.Id;
        Address = config.Address;
        Name = config.Name;
        IsEnabled = config.IsEnabled;
        
        // Start simulation timer for mock card reads
        _simulationTimer = new Timer(SimulateCardRead, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
    }
    
    public Guid Id { get; }
    public byte Address { get; }
    public string Name { get; }
    public bool IsOnline { get; private set; }
    public DateTime LastActivity { get; private set; } = DateTime.UtcNow;
    public bool IsEnabled { get; }

    public Task<bool> SendFeedbackAsync(ReaderFeedback feedback)
    {
        if (!IsOnline) return Task.FromResult(false);
        
        LastActivity = DateTime.UtcNow;
        
        // Log the feedback for testing purposes
        Console.WriteLine($"Mock device {Name}: LED={feedback.LedColor}, Duration={feedback.LedDuration}s, Beeps={feedback.BeepCount}");
        
        return Task.FromResult(true);
    }

    public event EventHandler<CardReadEvent>? CardRead;
    public event EventHandler<PinDigitEvent>? PinDigitReceived;
    public event EventHandler<OsdpStatusChangedEventArgs>? StatusChanged;
    
    public Task<bool> ConnectAsync()
    {
        if (!IsEnabled) return Task.FromResult(false);
        
        try
        {
            // Simulate the connection process
            IsOnline = true;
            LastActivity = DateTime.UtcNow;
            
            _logger.LogInformation("Mock OSDP device {DeviceName} connected on address {Address}", 
                Name, Address);
            
            StatusChanged?.Invoke(this, new OsdpStatusChangedEventArgs
            {
                IsOnline = true,
                Message = "Connected successfully"
            });
            
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to mock OSDP device {DeviceName}", Name);
            return Task.FromResult(false);
        }
    }
    
    public Task DisconnectAsync()
    {
        IsOnline = false;
        
        _logger.LogInformation("Mock OSDP device {DeviceName} disconnected", Name);
        
        StatusChanged?.Invoke(this, new OsdpStatusChangedEventArgs
        {
            IsOnline = false,
            Message = "Disconnected"
        });
        
        return Task.CompletedTask;
    }
    
   
    private void SimulateCardRead(object? state)
    {
        if (!IsOnline || !IsEnabled) return;
        
        // Randomly simulate card reads
        if (_random.Next(1, 100) <= 20) // 20% chance
        {
            var cardNumber = GenerateRandomCardNumber();
            var cardRead = new CardReadEvent
            {
                ReaderId = Id,
                CardNumber = cardNumber,
                BitLength = 26,
                Timestamp = DateTime.UtcNow,
                ReaderName = Name
            };
            
            _logger.LogInformation("Mock card read on device {DeviceName}: {CardNumber}", 
                Name, cardNumber);
            
            CardRead?.Invoke(this, cardRead);
            LastActivity = DateTime.UtcNow;
        }
    }
    
    private string GenerateRandomCardNumber()
    {
        // Generate a random 8-digit card number
        return _random.Next(10000000, 99999999).ToString();
    }
    
    public void Dispose()
    {
        _simulationTimer?.Dispose();
    }
}