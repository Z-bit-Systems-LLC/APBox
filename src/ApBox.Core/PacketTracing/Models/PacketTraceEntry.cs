using OSDP.Net.Messages;
using OSDP.Net.Model;
using OSDP.Net.Tracing;

namespace ApBox.Core.PacketTracing.Models;

/// <summary>
/// Represents an entry in a packet trace, providing details of a packet, including its direction,
/// timestamp, type, and associated data.
/// </summary>
public class PacketTraceEntry
{
    /// <summary>
    /// Gets the direction of the packet in the trace entry.
    /// This property indicates whether the packet is incoming or outgoing.
    /// </summary>
    public TraceDirection Direction { get; }

    /// <summary>
    /// Gets the timestamp associated with the packet trace entry.
    /// This property indicates the UTC time at which the packet was captured.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets the timestamp of the packet trace entry converted to local time.
    /// This property provides the local representation of the trace entry's timestamp.
    /// </summary>
    public DateTime LocalTimestamp => Timestamp.ToLocalTime();

    /// <summary>
    /// Represents the time interval between the current packet and the previous one within the packet trace.
    /// This property is useful for analyzing timing or delay in communication flows.
    /// </summary>
    public TimeSpan Interval { get; }

    /// <summary>
    /// Gets the type of the packet represented in the trace entry.
    /// Uses OSDP protocol-standard names like "osdp_POLL", "osdp_ACK".
    /// </summary>
    public string Type => Packet.CommandType?.GetDisplayName() ?? Packet.ReplyType?.GetDisplayName() ?? "Unknown";

    /// <summary>
    /// Gets the packet associated with the trace entry.
    /// This property contains the detailed information being traced,
    /// including the data and type of the packet.
    /// </summary>
    public Packet Packet { get; }

    /// <summary>
    /// Gets the security state of the packet.
    /// </summary>
    public PacketSecurityState SecurityState
    {
        get
        {
            if (!Packet.IsSecureMessage)
                return PacketSecurityState.ClearText;

            return Packet.IsUsingDefaultKey
                ? PacketSecurityState.SecureDefaultKey
                : PacketSecurityState.Secure;
        }
    }

    /// <summary>
    /// Gets the detailed information of the packet payload in the trace entry.
    /// This property parses and formats the payload data of the packet,
    /// or returns "Empty" if no data is available.
    /// </summary>
    public string Details
    {
        get
        {
            var payload = Packet.ParsePayloadData();
            if (payload == null) return "Empty";

            // Handle byte arrays specially - format as hex string
            if (payload is byte[] bytes)
            {
                return bytes.Length == 0 ? "Empty" : BitConverter.ToString(bytes);
            }

            return payload.ToString() ?? "Empty";
        }
    }

    // Private constructor
    private PacketTraceEntry(TraceDirection direction, DateTime timestamp, TimeSpan interval, Packet packet)
    {
        Direction = direction;
        Timestamp = timestamp;
        Interval = interval;
        Packet = packet;
    }

    // Factory method
    internal static PacketTraceEntry Create(TraceDirection direction, DateTime timestamp, TimeSpan interval, Packet packet)
    {
        return new PacketTraceEntry(direction, timestamp, interval, packet);
    }
}