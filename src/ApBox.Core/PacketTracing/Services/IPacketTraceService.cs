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
        void StartTracingAll();
        void StopTracingAll();
        void ClearTraces(string? readerId = null);
        
        IEnumerable<PacketTraceEntry> GetTraces(string? readerId = null, int? limit = null);
        
        void UpdateSettings(PacketTraceSettings settings);
        PacketTraceSettings GetCurrentSettings();
        TracingStatistics GetStatistics();
        
        // Method to capture packet from OSDP.Net TraceEntry
        void CapturePacket(TraceEntry traceEntry, string readerId, string readerName);
        
        // Legacy method to capture raw packet data (for backward compatibility)
        void CapturePacket(byte[] rawData, TraceDirection direction, string readerId, string readerName, byte address);
        
        event EventHandler<PacketTraceEntry>? PacketCaptured;
    }
}