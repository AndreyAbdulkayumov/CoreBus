using Core.Clients;
using Core.Clients.DataTypes;
using Core.Models.Modbus;
using Core.Models.NoProtocol;
using Core.Models.Settings;
using System.Text;

namespace Core.Models;

public class ConnectedHost
{
    public bool HostIsConnect
    {
        get => Client == null ? false : Client.IsConnected;
    }

    public bool DisconnectedByClient { get; private set; }

    public int Host_ReadTimeout
    {
        get => Client == null ? 0 : Client.ReadTimeout;
    }

    public event EventHandler<IConnection?>? DeviceIsConnect;
    public event EventHandler<IConnection?>? DeviceIsDisconnected;

    private readonly Model_NoProtocol NoProtocolModel;
    private readonly Model_Modbus ModbusModel;
    private readonly Model_Settings SettingsModel;

    public IConnection? Client { get; private set; }

    public static ProtocolMode? SelectedProtocol { get; private set; }

    // Значение кодировки по умолчанию
    public static Encoding GlobalEncoding { get; private set; } = Encoding.Default;


    public ConnectedHost(Model_NoProtocol noProtocolModel, Model_Modbus modbusModel, Model_Settings settingsModel)
    {
        NoProtocolModel = noProtocolModel ?? throw new ArgumentNullException(nameof(noProtocolModel));
        ModbusModel = modbusModel ?? throw new ArgumentNullException(nameof(modbusModel));
        SettingsModel = settingsModel ?? throw new ArgumentNullException(nameof(settingsModel));

        DeviceIsConnect += NoProtocolModel.Host_DeviceIsConnect;
        DeviceIsDisconnected += NoProtocolModel.Host_DeviceIsDisconnected;

        DeviceIsConnect += ModbusModel.Host_DeviceIsConnect;
        DeviceIsDisconnected += ModbusModel.Host_DeviceIsDisconnected;

        SetProtocol_NoProtocol();

        DeviceIsDisconnected?.Invoke(this, Client);
    }

    public void SetProtocol_NoProtocol()
    {
        SelectedProtocol = new ProtocolMode_NoProtocol(Client);
    }

    public void SetProtocol_Modbus()
    {
        SelectedProtocol = new ProtocolMode_Modbus(Client, SettingsModel.Settings);
    }

    public void SetGlobalEncoding(Encoding globalEncoding)
    {
        GlobalEncoding = globalEncoding;
    }

    public void Connect(ConnectionInfo information)
    {
        if (SelectedProtocol == null)
        {
            throw new Exception("Не выбран протокол.");
        }

        if (information.Info as SerialPortInfo != null)
        {
            Client = new SerialPortClient();
        }

        else if (information.Info as SocketInfo != null)
        {
            Client = new IPClient();
        }

        else
        {
            throw new Exception("В файле настроек задан неизвестный интерфейс связи.");
        }

        Client.Connect(information);

        DisconnectedByClient = false;

        SetGlobalEncoding(GlobalEncoding);

        var modbusProtocol = SelectedProtocol as ProtocolMode_Modbus;

        modbusProtocol?.UpdateTimeouts(SettingsModel.Settings);

        SelectedProtocol.InitMode(Client);

        DeviceIsConnect?.Invoke(this, Client);
    }

    public async Task Disconnect()
    {
        if (Client == null)
        {
            return;
        }

        try
        {
            DisconnectedByClient = true;
            await Client.Disconnect();
        }
        
        catch (Exception)
        {
            DisconnectedByClient = false;
            throw;
        }        

        DeviceIsDisconnected?.Invoke(this, Client);
    }
}
