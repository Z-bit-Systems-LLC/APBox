using ApBox.Core.Models;
using ApBox.Plugins;

namespace ApBox.Web.Services;

/// <summary>
/// Orchestrates the card processing workflow without handling specific concerns
/// </summary>
public interface ICardProcessingOrchestrator
{
    /// <summary>
    /// Orchestrates the complete card processing workflow
    /// </summary>
    /// <param name="cardRead">The card read event to process</param>
    /// <returns>The result of the card processing</returns>
    Task<CardReadResult> OrchestrateCardProcessingAsync(CardReadEvent cardRead);
}