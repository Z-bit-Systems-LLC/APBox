using ApBox.Core.PacketTracing.Models;

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
        
        IEnumerable<PacketTraceEntry> GetTraces(string? readerId = null, int? limit = null, bool filterPollCommands = false, bool filterAckCommands = false);
        
        void UpdateSettings(PacketTraceSettings settings);
        PacketTraceSettings GetCurrentSettings();
        TracingStatistics GetStatistics();
        
        // Method to capture raw packet data
        void CapturePacket(byte[] rawData, PacketDirection direction, string readerId, string readerName, byte address);
        
        event EventHandler<PacketTraceEntry>? PacketCaptured;
    }
}