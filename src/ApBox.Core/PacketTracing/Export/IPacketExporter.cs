using ApBox.Core.PacketTracing.Models;

namespace ApBox.Core.PacketTracing.Export;

/// <summary>
/// Interface for exporting packet trace data to various formats.
/// </summary>
public interface IPacketExporter
{
    /// <summary>
    /// Gets the file extension for this exporter (e.g., ".osdpcap", ".txt").
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// Gets the content type for this exporter (e.g., "application/octet-stream", "text/plain").
    /// </summary>
    string ContentType { get; }

    /// <summary>
    /// Gets a display name for this export format.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Exports packet trace entries to bytes.
    /// </summary>
    /// <param name="packets">The packets to export.</param>
    /// <returns>The exported data as bytes.</returns>
    Task<byte[]> ExportAsync(IEnumerable<PacketTraceEntry> packets);
}
