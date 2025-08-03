using ApBox.Core.Models;
using ApBox.Plugins;

namespace ApBox.Core.Services.Reader;

/// <summary>
/// Service for managing OSDP readers, including configuration, connection management,
/// and status monitoring.
/// </summary>
public interface IReaderService
{
    /// <summary>
    /// Get all configured readers.
    /// </summary>
    /// <returns>Collection of all reader configurations</returns>
    Task<IEnumerable<ReaderConfiguration>> GetReadersAsync();
    
    /// <summary>
    /// Get a specific reader configuration by ID.
    /// </summary>
    /// <param name="readerId">The unique identifier of the reader</param>
    /// <returns>The reader configuration, or null if not found</returns>
    Task<ReaderConfiguration?> GetReaderAsync(Guid readerId);
    
    /// <summary>
    /// Update an existing reader configuration.
    /// </summary>
    /// <param name="reader">The updated reader configuration</param>
    /// <returns>A task representing the update operation</returns>
    Task UpdateReaderAsync(ReaderConfiguration reader);
    
    /// <summary>
    /// Send feedback (LED, beep, display) to a specific reader.
    /// </summary>
    /// <param name="readerId">The ID of the reader to send feedback to</param>
    /// <param name="feedback">The feedback configuration to send</param>
    /// <returns>True if feedback was sent successfully, false otherwise</returns>
    Task<bool> SendFeedbackAsync(Guid readerId, ReaderFeedback feedback);
    
    // OSDP Integration
    
    /// <summary>
    /// Establish OSDP connection to a reader.
    /// </summary>
    /// <param name="readerId">The ID of the reader to connect to</param>
    /// <returns>True if connection was established successfully, false otherwise</returns>
    Task<bool> ConnectReaderAsync(Guid readerId);
    
    /// <summary>
    /// Disconnect from a reader.
    /// </summary>
    /// <param name="readerId">The ID of the reader to disconnect from</param>
    /// <returns>True if disconnection was successful, false otherwise</returns>
    Task<bool> DisconnectReaderAsync(Guid readerId);
    
    /// <summary>
    /// Test the connection to a reader.
    /// </summary>
    /// <param name="readerId">The ID of the reader to test</param>
    /// <returns>True if the reader responds to the test, false otherwise</returns>
    Task<bool> TestConnectionAsync(Guid readerId);
    
    /// <summary>
    /// Install a secure channel key on a reader for encrypted communication.
    /// </summary>
    /// <param name="readerId">The ID of the reader to install the key on</param>
    /// <returns>True if key installation was successful, false otherwise</returns>
    Task<bool> InstallSecureKeyAsync(Guid readerId);
    
    /// <summary>
    /// Refresh connections to all configured readers.
    /// </summary>
    /// <returns>A task representing the refresh operation</returns>
    Task RefreshAllReadersAsync();
    
    // Status Information
    
    /// <summary>
    /// Check if a specific reader is currently online and responding.
    /// </summary>
    /// <param name="readerId">The ID of the reader to check</param>
    /// <returns>True if the reader is online, false otherwise</returns>
    Task<bool> GetReaderOnlineStatusAsync(Guid readerId);
    
    /// <summary>
    /// Get the online status of all configured readers.
    /// </summary>
    /// <returns>Dictionary mapping reader IDs to their online status</returns>
    Task<Dictionary<Guid, bool>> GetAllReaderStatusesAsync();
}