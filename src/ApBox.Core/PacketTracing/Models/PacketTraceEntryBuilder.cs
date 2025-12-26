using OSDP.Net.Tracing;

namespace ApBox.Core.PacketTracing.Models;

/// <summary>
/// A builder class used to construct instances of <see cref="PacketTraceEntry"/>.
/// </summary>
public class PacketTraceEntryBuilder
{
    private readonly MessageSpy _messageSpy = new();
    private TraceEntry _traceEntry;
    private PacketTraceEntry? _lastTraceEntry;
    private DateTime _timestamp;

    /// <summary>
    /// Initializes the <see cref="PacketTraceEntryBuilder"/> instance with the specified trace entry and previous trace entry
    /// while also recording the packet reception timestamp.
    /// </summary>
    /// <param name="traceEntry">The current trace entry that provides details for creating a new trace packet.</param>
    /// <param name="lastTraceEntry">The previous packet trace entry used for calculating the interval; can be null.</param>
    /// <param name="receptionTimestamp">The timestamp when the packet was received. If null, uses current time.</param>
    /// <returns>The current <see cref="PacketTraceEntryBuilder"/> instance, updated with the provided trace entry details.</returns>
    public PacketTraceEntryBuilder FromTraceEntry(TraceEntry traceEntry, PacketTraceEntry? lastTraceEntry, DateTime? receptionTimestamp = null)
    {
        _traceEntry = traceEntry;
        _lastTraceEntry = lastTraceEntry;
        _timestamp = receptionTimestamp ?? DateTime.UtcNow;

        return this;
    }

    /// <summary>
    /// Creates and returns a new instance of the <see cref="PacketTraceEntry"/> class.
    /// Uses MessageSpy.TryParsePacket for exception-free parsing.
    /// </summary>
    /// <returns>A new instance of the <see cref="PacketTraceEntry"/> class, or null if the packet could not be parsed.</returns>
    public PacketTraceEntry? Build()
    {
        if (!_messageSpy.TryParsePacket(_traceEntry.Data, out var packet) || packet == null)
        {
            return null; // Return null for unparseable packets
        }

        return PacketTraceEntry.Create(_traceEntry.Direction, _timestamp,
            _lastTraceEntry != null ? _timestamp - _lastTraceEntry.Timestamp : TimeSpan.Zero,
            packet);
    }
}