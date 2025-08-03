using ApBox.Core.Models;
using ApBox.Plugins;

namespace ApBox.Core.Services.Reader;

public interface IReaderService
{
    Task<IEnumerable<ReaderConfiguration>> GetReadersAsync();
    Task<ReaderConfiguration?> GetReaderAsync(Guid readerId);
    Task UpdateReaderAsync(ReaderConfiguration reader);
    Task<bool> SendFeedbackAsync(Guid readerId, ReaderFeedback feedback);
    
    // OSDP Integration
    Task<bool> ConnectReaderAsync(Guid readerId);
    Task<bool> DisconnectReaderAsync(Guid readerId);
    Task<bool> TestConnectionAsync(Guid readerId);
    Task<bool> InstallSecureKeyAsync(Guid readerId);
    Task RefreshAllReadersAsync();
    
    // Status Information
    Task<bool> GetReaderOnlineStatusAsync(Guid readerId);
    Task<Dictionary<Guid, bool>> GetAllReaderStatusesAsync();
}