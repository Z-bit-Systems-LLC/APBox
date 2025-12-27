using ApBox.Core.PacketTracing.Models;
using OSDP.Net.Tracing;

namespace ApBox.Core.PacketTracing.Services
{
    public interface IPacketTraceService
    {
        bool IsTracing { get; }
        bool IsTracingReader(string readerId);

        void StartTracing(string readerId);
        void StopTracing(string readerId);
        Task StartTracingAll();
        void StopTracingAll();
        void ClearTraces(string? readerId = null);

        IEnumerable<PacketTraceEntry> GetTraces(string? readerId = null, int? limit = null);

        void UpdateSettings(PacketTraceSettings settings);
        PacketTraceSettings GetCurrentSettings();
        TracingStatistics GetStatistics();

        /// <summary>
        /// Sets the security key for a reader to enable decryption of secure channel packets.
        /// </summary>
        /// <param name="readerId">The reader ID.</param>
        /// <param name="securityKey">The 16-byte security key, or null to disable decryption.</param>
        void SetSecurityKey(string readerId, byte[]? securityKey);

        /// <summary>
        /// Gets the security key for a reader, if one has been set.
        /// </summary>
        byte[]? GetSecurityKey(string readerId);

        // Method to capture packet from OSDP.Net TraceEntry
        void CapturePacket(TraceEntry traceEntry, string readerId, string readerName);

        event EventHandler<PacketTraceEntry>? PacketCaptured;
    }
}