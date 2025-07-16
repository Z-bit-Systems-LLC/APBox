using System.IO.Ports;
using OSDP.Net.Connections;

namespace ApBox.Core.Services;

/// <summary>
/// Production implementation of ISerialPortService using actual serial ports
/// </summary>
public class SerialPortService : ISerialPortService
{
    public string[] GetAvailablePortNames()
    {
        try
        {
            return SerialPort.GetPortNames();
        }
        catch (Exception)
        {
            // Return empty array if unable to get port names
            return Array.Empty<string>();
        }
    }

    public bool PortExists(string portName)
    {
        if (string.IsNullOrEmpty(portName))
            return false;

        try
        {
            var availablePorts = GetAvailablePortNames();
            return availablePorts.Contains(portName);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public SerialPortOsdpConnection CreateConnection(string portName, int baudRate)
    {
        return new SerialPortOsdpConnection(portName, baudRate);
    }
}