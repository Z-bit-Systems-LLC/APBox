using ApBox.Core.Models;
using ApBox.Plugins;

namespace ApBox.Core.Services.Core;

public interface ICardProcessingService
{
    Task<CardReadResult> ProcessCardReadAsync(CardReadEvent cardRead);
    Task<ReaderFeedback> GetFeedbackAsync(Guid readerId, CardReadResult result);
}