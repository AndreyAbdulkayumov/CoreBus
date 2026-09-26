using System.Text;

namespace Core.Clients.DataTypes;

public interface ITypeOfInfo
{

}

public class SocketInfo : ITypeOfInfo
{
    public string? IP;
    public string? Port;

    public SocketInfo(string? ip, string? port)
    {
        IP = ip;
        Port = port;
    }
}

public class SerialPortInfo : ITypeOfInfo
{
    public string? Port;
    public string? BaudRate;
    public string? Parity;
    public string? DataBits;
    public string? StopBits;

    public SerialPortInfo(string? port, string? baudRate, string? parity, string? dataBits, string? stopBits)
    {
        Port = port;
        BaudRate = baudRate;
        Parity = parity;
        DataBits = dataBits;
        StopBits = stopBits;
    }
}

public class ConnectionInfo
{
    public readonly ITypeOfInfo Info;

    public readonly Encoding GlobalEncoding;

    public ConnectionInfo(SocketInfo info, Encoding globalEncoding)
    {
        Info = info;
        GlobalEncoding = globalEncoding;
    }

    public ConnectionInfo(SerialPortInfo info, Encoding globalEncoding)
    {
        Info = info;
        GlobalEncoding = globalEncoding;
    }
}
