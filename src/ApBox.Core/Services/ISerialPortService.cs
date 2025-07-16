using OSDP.Net.Connections;

namespace ApBox.Core.Services;

/// <summary>
/// Abstraction for serial port operations to enable mocking in tests
/// </summary>
public interface ISerialPortService
{
    /// <summary>
    /// Gets the names of available serial ports
    /// </summary>
    /// <returns>Array of serial port names</returns>
    string[] GetAvailablePortNames();
    
    /// <summary>
    /// Checks if a serial port exists
    /// </summary>
    /// <param name="portName">The port name to check</param>
    /// <returns>True if the port exists, false otherwise</returns>
    bool PortExists(string portName);
    
    /// <summary>
    /// Creates an OSDP serial port connection
    /// </summary>
    /// <param name="portName">The serial port name</param>
    /// <param name="baudRate">The baud rate</param>
    /// <returns>An OSDP serial port connection</returns>
    SerialPortOsdpConnection CreateConnection(string portName, int baudRate);
}