using ApBox.Plugins;
using System.Collections.Concurrent;

namespace ApBox.Core.Services;

public class PinCollectionService : IPinCollectionService, IDisposable
{
    private readonly ILogger<PinCollectionService> _logger;
    private readonly IReaderConfigurationService _readerConfigurationService;
    private readonly ConcurrentDictionary<Guid, PinCollection> _activePinCollections = new();
    private readonly TimeSpan _pinTimeout = TimeSpan.FromSeconds(3);
    private bool _disposed;

    public event EventHandler<PinReadEvent>? PinCollectionCompleted;
    public event EventHandler<PinDigitEvent>? PinDigitReceived;

    public PinCollectionService(
        ILogger<PinCollectionService> logger,
        IReaderConfigurationService readerConfigurationService)
    {
        _logger = logger;
        _readerConfigurationService = readerConfigurationService;
    }

    public async Task<bool> AddDigitAsync(Guid readerId, char digit)
    {
        if (_disposed) return false;

        var collection = _activePinCollections.GetOrAdd(readerId, _ => new PinCollection(readerId, _pinTimeout, OnPinTimeout));
        
        var isComplete = await collection.AddDigitAsync(digit);
        
        // Fire digit received event
        PinDigitReceived?.Invoke(this, new PinDigitEvent
        {
            ReaderId = readerId,
            Digit = digit,
            Timestamp = DateTime.UtcNow,
            SequenceNumber = collection.DigitCount
        });

        if (isComplete)
        {
            await CompletePinCollection(collection);
        }

        return isComplete;
    }

    public Task<string?> GetCurrentPinAsync(Guid readerId)
    {
        if (_activePinCollections.TryGetValue(readerId, out var collection))
        {
            return Task.FromResult<string?>(collection.CurrentPin);
        }
        return Task.FromResult<string?>(null);
    }

    public Task ClearPinAsync(Guid readerId)
    {
        if (_activePinCollections.TryRemove(readerId, out var collection))
        {
            collection.Dispose();
        }
        return Task.CompletedTask;
    }

    private async Task OnPinTimeout(PinCollection collection)
    {
        _logger.LogDebug("PIN collection timeout for reader {ReaderId}", collection.ReaderId);
        
        if (_activePinCollections.TryRemove(collection.ReaderId, out _))
        {
            await CompletePinCollection(collection, PinCompletionReason.Timeout);
        }
    }

    private async Task CompletePinCollection(PinCollection collection, PinCompletionReason? reason = null)
    {
        try
        {
            var completionReason = reason ?? (collection.CompletedByPound ? PinCompletionReason.PoundKey : PinCompletionReason.Timeout);
            
            // Look up reader name from configuration
            string readerName = string.Empty;
            try
            {
                var readerConfig = await _readerConfigurationService.GetReaderAsync(collection.ReaderId);
                readerName = readerConfig?.ReaderName ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get reader name for {ReaderId}, using empty name", collection.ReaderId);
            }
            
            var pinReadEvent = new PinReadEvent
            {
                ReaderId = collection.ReaderId,
                ReaderName = readerName,
                Pin = collection.CurrentPin,
                Timestamp = DateTime.UtcNow,
                CompletionReason = completionReason
            };

            _logger.LogInformation("PIN collection completed for reader {ReaderName} ({ReaderId}), reason: {Reason}, length: {Length}", 
                readerName, collection.ReaderId, completionReason, collection.CurrentPin.Length);

            PinCollectionCompleted?.Invoke(this, pinReadEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing PIN collection for reader {ReaderId}", collection.ReaderId);
        }
        finally
        {
            // Ensure collection is removed and disposed
            _activePinCollections.TryRemove(collection.ReaderId, out _);
            collection.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        
        // Dispose all active collections
        foreach (var collection in _activePinCollections.Values)
        {
            collection.Dispose();
        }
        _activePinCollections.Clear();
    }

    private class PinCollection : IDisposable
    {
        private readonly System.Threading.Timer _timer;
        private readonly object _lock = new();
        private readonly Func<PinCollection, Task> _onTimeout;
        private string _currentPin = string.Empty;
        private bool _disposed;
        private bool _completedByPound;

        public Guid ReaderId { get; }
        public string CurrentPin 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    return _currentPin; 
                } 
            } 
        }
        public int DigitCount 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    return _currentPin.Length; 
                } 
            } 
        }
        public bool CompletedByPound => _completedByPound;

        public PinCollection(Guid readerId, TimeSpan timeout, Func<PinCollection, Task> onTimeout)
        {
            ReaderId = readerId;
            _onTimeout = onTimeout;
            _timer = new System.Threading.Timer(OnTimerElapsed, null, timeout, Timeout.InfiniteTimeSpan);
        }

        public Task<bool> AddDigitAsync(char digit)
        {
            if (_disposed) return Task.FromResult(false);

            lock (_lock)
            {
                if (_disposed) return Task.FromResult(false);

                // Check for pound key (completion)
                if (digit == '#')
                {
                    _completedByPound = true;
                    _timer.Dispose();
                    return Task.FromResult(true);
                }

                // Check for asterisk key (clear)
                if (digit == '*')
                {
                    _currentPin = string.Empty;
                    
                    // Reset the timer when clearing
                    _timer.Change(TimeSpan.FromSeconds(3), Timeout.InfiniteTimeSpan);
                    return Task.FromResult(false);
                }

                // Only add numeric digits
                if (char.IsDigit(digit))
                {
                    _currentPin += digit;
                    
                    // Reset the timer for each new digit
                    _timer.Change(TimeSpan.FromSeconds(3), Timeout.InfiniteTimeSpan);
                }

                return Task.FromResult(false);
            }
        }

        private async void OnTimerElapsed(object? state)
        {
            if (_disposed) return;
            
            try
            {
                await _onTimeout(this);
            }
            catch (Exception)
            {
                // Ignore exceptions in timer callback to prevent crashes
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
                
                _timer.Dispose();
                
                // Clear PIN from memory for security
                _currentPin = string.Empty;
            }
        }
    }
}