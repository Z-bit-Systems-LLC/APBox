namespace ApBox.Core.PacketTracing.Models
{
    public class PacketTraceEntryBuilder
    {
        private byte[]? _rawData;
        private PacketDirection _direction;
        private string _readerId = string.Empty;
        private string _readerName = string.Empty;
        private byte _address;
        private PacketTraceEntry? _previousEntry;
        
        public PacketTraceEntryBuilder FromRawData(byte[] rawData)
        {
            _rawData = rawData;
            return this;
        }
        
        public PacketTraceEntryBuilder WithDirection(PacketDirection direction)
        {
            _direction = direction;
            return this;
        }
        
        public PacketTraceEntryBuilder WithReader(string readerId, string readerName, byte address)
        {
            _readerId = readerId;
            _readerName = readerName;
            _address = address;
            return this;
        }
        
        public PacketTraceEntryBuilder WithPreviousEntry(PacketTraceEntry? previousEntry)
        {
            _previousEntry = previousEntry;
            return this;
        }
        
        public PacketTraceEntry Build()
        {
            if (_rawData == null)
                throw new InvalidOperationException("Raw data is required");
                
            var entry = PacketTraceEntry.Create(
                _rawData, 
                _direction, 
                _readerId, 
                _readerName, 
                _address, 
                _previousEntry);
            
            // Parse OSDP packet details
            ParseOsdpPacketDetails(entry, _rawData);
            
            return entry;
        }

        private static void ParseOsdpPacketDetails(PacketTraceEntry entry, byte[] rawData)
        {
            try
            {
                if (rawData.Length < 4)
                {
                    entry.Type = "Unknown";
                    entry.Details = "Packet too short";
                    entry.IsValid = false;
                    return;
                }

                // Basic OSDP packet structure validation
                if (rawData[0] == 0x53) // OSDP SOM (Start of Message)
                {
                    entry.IsValid = true;
                    
                    if (rawData.Length > 4)
                    {
                        var commandByte = rawData[4];
                        entry.Type = GetOsdpCommandName(commandByte);
                        entry.Command = $"0x{commandByte:X2}";
                        
                        // Add more details based on command type
                        entry.Details = $"OSDP {entry.Type} - Address: {rawData[1]:X2}, Length: {rawData.Length}";
                    }
                    else
                    {
                        entry.Type = "OSDP Packet";
                        entry.Details = $"OSDP packet - Address: {rawData[1]:X2}, Length: {rawData.Length}";
                    }
                }
                else
                {
                    // Not a standard OSDP packet, could be raw card data
                    entry.Type = "Raw Data";
                    entry.Details = $"Raw data packet - Length: {rawData.Length} bytes";
                    entry.IsValid = true; // Raw data is still valid
                }
            }
            catch (Exception)
            {
                entry.Type = "Parse Error";
                entry.Details = "Failed to parse packet";
                entry.IsValid = false;
            }
        }

        private static string GetOsdpCommandName(byte commandByte)
        {
            return commandByte switch
            {
                0x40 => "ACK",
                0x41 => "NAK",
                0x45 => "PDID",
                0x46 => "PDCAP",
                0x48 => "LSTATR",
                0x49 => "ISTATR",
                0x4A => "OSTATR",
                0x4B => "RSTATR",
                0x50 => "RAW",
                0x51 => "FMT",
                0x52 => "KEYPAD",
                0x53 => "COM",
                0x54 => "MFG",
                0x60 => "POLL",
                0x61 => "ID",
                0x62 => "CAP",
                0x64 => "LSTAT",
                0x65 => "ISTAT",
                0x66 => "OSTAT",
                0x67 => "RSTAT",
                0x69 => "LED",
                0x6A => "BUZ",
                0x6B => "TEXT",
                0x6C => "RMODE",
                0x6E => "TDSET",
                0x6F => "COMSET",
                0x75 => "KEYSET",
                0x76 => "CRYPT",
                _ => $"Unknown (0x{commandByte:X2})"
            };
        }
    }
}