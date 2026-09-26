using Core.Clients.DataTypes;
using Core.Models.Settings.FileTypes;

namespace Core.Models;

public abstract class ProtocolMode
{
    protected int WriteTimeout { get; set; } = Timeout.Infinite;
    protected int ReadTimeout { get; set; } = Timeout.Infinite;

    public ReadMode CurrentReadMode { get; protected set; } = ReadMode.Async;

    public void InitMode(IConnection? client)
    {
        if (client == null || client.IsConnected == false)
        {
            return;
        }

        client.SetReadMode(CurrentReadMode);

        client.WriteTimeout = WriteTimeout;
        client.ReadTimeout = ReadTimeout;
    }
}

public class ProtocolMode_NoProtocol : ProtocolMode
{
    public ProtocolMode_NoProtocol(IConnection? client)
    {
        CurrentReadMode = ReadMode.Async;

        WriteTimeout = 500;
        ReadTimeout = Timeout.Infinite;

        InitMode(client);
    }
}

public class ProtocolMode_Modbus : ProtocolMode
{
    public ProtocolMode_Modbus(IConnection? client, DeviceData? settings)
    {
        CurrentReadMode = ReadMode.Sync;

        UpdateTimeouts(settings);

        InitMode(client);
    }

    public void UpdateTimeouts(DeviceData? settings)
    {
        if (settings != null)
        {
            WriteTimeout = Convert.ToInt32(settings.TimeoutWrite);
            ReadTimeout = Convert.ToInt32(settings.TimeoutRead);
        }
    }
}
