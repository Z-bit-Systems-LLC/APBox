using ApBox.Core.Services.Core;
using ApBox.Core.Services.Events;
using ApBox.Core.Data.Repositories;
using ApBox.Core.Models;
using ApBox.Plugins;

namespace ApBox.Core.Services.Infrastructure;

/// <summary>
/// Unified event processing pipeline that orchestrates all event processing
/// and publishes completion events
/// </summary>
public class EventProcessingPipeline : IEventProcessingPipeline
{
    private readonly CardEventProcessingOrchestrator _cardOrchestrator;
    private readonly PinEventProcessingOrchestrator _pinOrchestrator;
    private readonly IEventPublisher _eventPublisher;
    private readonly IReaderConfigurationRepository _readerConfigRepository;
    private readonly ILogger<EventProcessingPipeline> _logger;

    public EventProcessingPipeline(
        CardEventProcessingOrchestrator cardOrchestrator,
        PinEventProcessingOrchestrator pinOrchestrator,
        IEventPublisher eventPublisher,
        IReaderConfigurationRepository readerConfigRepository,
        ILogger<EventProcessingPipeline> logger)
    {
        _cardOrchestrator = cardOrchestrator;
        _pinOrchestrator = pinOrchestrator;
        _eventPublisher = eventPublisher;
        _readerConfigRepository = readerConfigRepository;
        _logger = logger;
    }

    public async Task ProcessCardEventAsync(CardReadEvent cardRead)
    {
        _logger.LogInformation("Processing card event for reader {ReaderId}, card {CardNumber}", 
            cardRead.ReaderId, cardRead.CardNumber);

        try
        {
            // Process through core orchestrator
            var result = await _cardOrchestrator.ProcessEventAsync(cardRead);

            // Publish completion event
            var completionEvent = new CardProcessingCompletedEvent
            {
                CardRead = cardRead,
                Result = result.PluginResult,
                Feedback = result.Feedback,
                PersistenceSuccessful = result.PersistenceSuccessful,
                FeedbackDeliverySuccessful = result.FeedbackDeliverySuccessful
            };

            await _eventPublisher.PublishAsync(completionEvent);

            _logger.LogInformation("Card event processing completed for {CardNumber}: {Success}", 
                cardRead.CardNumber, result.PluginResult.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in card event processing pipeline for {CardNumber}", 
                cardRead.CardNumber);
        }
    }

    public async Task ProcessPinEventAsync(PinReadEvent pinRead)
    {
        _logger.LogInformation("Processing pin event for reader {ReaderId}", pinRead.ReaderId);

        try
        {
            // Process through core orchestrator
            var result = await _pinOrchestrator.ProcessEventAsync(pinRead);

            // Publish completion event
            var completionEvent = new PinProcessingCompletedEvent
            {
                PinRead = pinRead,
                Result = result.PluginResult,
                Feedback = result.Feedback,
                PersistenceSuccessful = result.PersistenceSuccessful,
                FeedbackDeliverySuccessful = result.FeedbackDeliverySuccessful
            };

            await _eventPublisher.PublishAsync(completionEvent);

            _logger.LogInformation("Pin event processing completed for reader {ReaderId}: {Success}", 
                pinRead.ReaderId, result.PluginResult.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in pin event processing pipeline for reader {ReaderId}", 
                pinRead.ReaderId);
        }
    }

    public async Task ProcessReaderStatusChangedAsync(ReaderStatusChangedEvent statusEvent)
    {
        _logger.LogInformation("Processing reader status change for {ReaderId}: {IsOnline}", 
            statusEvent.ReaderId, statusEvent.IsOnline);

        try
        {
            // Enrich the status event with database information
            var enrichedEvent = await EnrichReaderStatusEventAsync(statusEvent);
            
            // Publish the enriched event
            await _eventPublisher.PublishAsync(enrichedEvent);

            _logger.LogDebug("Enriched reader status change event published for {ReaderId}", 
                statusEvent.ReaderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing reader status change event for {ReaderId}", 
                statusEvent.ReaderId);
        }
    }
    
    private async Task<ReaderStatusChangedEvent> EnrichReaderStatusEventAsync(ReaderStatusChangedEvent originalEvent)
    {
        try
        {
            // Get reader configuration from repository for enrichment
            var readers = await _readerConfigRepository.GetAllAsync();
            var reader = readers.FirstOrDefault(r => r.ReaderId == originalEvent.ReaderId);
            
            if (reader != null)
            {
                return new ReaderStatusChangedEvent
                {
                    Timestamp = originalEvent.Timestamp,
                    ReaderId = originalEvent.ReaderId,
                    ReaderName = reader.ReaderName, // Use database name instead
                    IsOnline = originalEvent.IsOnline,
                    ErrorMessage = originalEvent.ErrorMessage,
                    
                    // Enriched properties from database
                    IsEnabled = reader.IsEnabled,
                    SecurityMode = reader.SecurityMode,
                    LastActivity = originalEvent.Timestamp, // Use event timestamp as last activity
                    Status = originalEvent.ErrorMessage ?? (originalEvent.IsOnline ? "Online" : "Offline")
                };
            }
            else
            {
                // If reader not found in database, return original with defaults
                return new ReaderStatusChangedEvent
                {
                    Timestamp = originalEvent.Timestamp,
                    ReaderId = originalEvent.ReaderId,
                    ReaderName = originalEvent.ReaderName,
                    IsOnline = originalEvent.IsOnline,
                    ErrorMessage = originalEvent.ErrorMessage,
                    
                    // Default values when not in database
                    IsEnabled = false,
                    SecurityMode = OsdpSecurityMode.ClearText,
                    LastActivity = originalEvent.Timestamp,
                    Status = originalEvent.ErrorMessage ?? (originalEvent.IsOnline ? "Online" : "Offline")
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enrich reader status event for {ReaderId}, using original event", 
                originalEvent.ReaderId);
            
            // Return original event with minimal enrichment on error
            return new ReaderStatusChangedEvent
            {
                Timestamp = originalEvent.Timestamp,
                ReaderId = originalEvent.ReaderId,
                ReaderName = originalEvent.ReaderName,
                IsOnline = originalEvent.IsOnline,
                ErrorMessage = originalEvent.ErrorMessage,
                IsEnabled = false,
                SecurityMode = OsdpSecurityMode.ClearText,
                LastActivity = originalEvent.Timestamp,
                Status = originalEvent.ErrorMessage ?? (originalEvent.IsOnline ? "Online" : "Offline")
            };
        }
    }
}