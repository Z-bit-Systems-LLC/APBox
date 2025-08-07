using ApBox.Core.Models;
using ApBox.Plugins;

namespace ApBox.Core.Services.Core;

/// <summary>
/// Service responsible for processing card read events through the plugin system
/// and generating appropriate reader feedback.
/// </summary>
public interface ICardProcessingService
{
    /// <summary>
    /// Process a card read event through all configured plugins for the reader.
    /// </summary>
    /// <param name="cardRead">The card read event to process</param>
    /// <returns>The result of processing the card read through the plugin system</returns>
    Task<CardReadResult> ProcessCardReadAsync(CardReadEvent cardRead);
    
}