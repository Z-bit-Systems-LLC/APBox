using ApBox.Core.Services;
using OSDP.Net.Connections;

namespace ApBox.Core.Tests.Mocks;

/// <summary>
/// Mock implementation of ISerialPortService for testing
/// </summary>
public class MockSerialPortService : ISerialPortService
{
    private readonly List<string> _availablePorts;
    private readonly Dictionary<string, SerialPortOsdpConnection> _connections;

    public MockSerialPortService()
    {
        _availablePorts = new List<string> { "COM1", "COM2", "COM3", "/dev/ttyUSB0", "/dev/ttyUSB1" };
        _connections = new Dictionary<string, SerialPortOsdpConnection>();
    }

    /// <summary>
    /// Add a mock port to the available ports list
    /// </summary>
    public void AddAvailablePort(string portName)
    {
        if (!_availablePorts.Contains(portName))
        {
            _availablePorts.Add(portName);
        }
    }

    /// <summary>
    /// Remove a mock port from the available ports list
    /// </summary>
    public void RemoveAvailablePort(string portName)
    {
        _availablePorts.Remove(portName);
    }

    /// <summary>
    /// Clear all available ports (simulate no ports available)
    /// </summary>
    public void ClearAvailablePorts()
    {
        _availablePorts.Clear();
    }

    public string[] GetAvailablePortNames()
    {
        return _availablePorts.ToArray();
    }

    public bool PortExists(string portName)
    {
        return !string.IsNullOrEmpty(portName) && _availablePorts.Contains(portName);
    }

    public SerialPortOsdpConnection CreateConnection(string portName, int baudRate)
    {
        if (!PortExists(portName))
        {
            throw new ArgumentException($"Port {portName} does not exist", nameof(portName));
        }

        // For testing, we'll try to create a connection but handle failures gracefully
        try
        {
            var connection = new SerialPortOsdpConnection(portName, baudRate);
            _connections[portName] = connection;
            return connection;
        }
        catch (Exception)
        {
            // In test environments, SerialPortOsdpConnection may fail to create
            // We'll create a mock connection that will fail when actually used
            // This is expected behavior for unit tests
            var mockConnection = new SerialPortOsdpConnection("COM1", 9600);
            _connections[portName] = mockConnection;
            return mockConnection;
        }
    }

    /// <summary>
    /// Get all created connections for testing verification
    /// </summary>
    public Dictionary<string, SerialPortOsdpConnection> GetCreatedConnections()
    {
        return new Dictionary<string, SerialPortOsdpConnection>(_connections);
    }

    /// <summary>
    /// Clear all created connections
    /// </summary>
    public void ClearConnections()
    {
        _connections.Clear();
    }
}