using ReactiveUI.Reactive;
using System.Reactive.Linq;
using Core.Models.Settings.FileTypes;

namespace ViewModels.Settings.Tabs;

public class Connection_VM : ReactiveObject
{
    private bool _selected_SerialPort;

    public bool Selected_SerialPort
    {
        get => _selected_SerialPort;
        set => this.RaiseAndSetIfChanged(ref _selected_SerialPort, value);
    }

    private bool _selected_Ethernet;

    public bool Selected_Ethernet
    {
        get => _selected_Ethernet;
        set => this.RaiseAndSetIfChanged(ref _selected_Ethernet, value);
    }

    private object? _currentConnectionViewModel;

    public object? CurrentConnectionViewModel
    {
        get => _currentConnectionViewModel;
        set => this.RaiseAndSetIfChanged(ref _currentConnectionViewModel, value);
    }

    private readonly Connection_SerialPort_VM _connection_SerialPort_VM;
    private readonly Connection_Ethernet_VM _connection_Ethernet_VM;


    public Connection_VM(Connection_SerialPort_VM connectionSerialPortVM, Connection_Ethernet_VM connectionEthernetVM)
    {
        _connection_SerialPort_VM = connectionSerialPortVM ?? throw new ArgumentNullException(nameof(connectionSerialPortVM));
        _connection_Ethernet_VM = connectionEthernetVM ?? throw new ArgumentNullException(nameof(connectionEthernetVM));

        this.WhenAnyValue(x => x.Selected_SerialPort)
            .Where(x => x == true)
            .Subscribe(x =>
            {
                CurrentConnectionViewModel = _connection_SerialPort_VM;
            });

        this.WhenAnyValue(x => x.Selected_Ethernet)
            .Where(x => x == true)
            .Subscribe(x =>
            {
                CurrentConnectionViewModel = _connection_Ethernet_VM;
            });
    }

    public ReactiveObject GetActiveTab()
    {
        if (Selected_SerialPort)
        {
            return _connection_SerialPort_VM;
        }

        return _connection_Ethernet_VM;
    }

    public void SettingsFileChanged()
    {
        _connection_SerialPort_VM.SettingsFileChanged();
        _connection_Ethernet_VM.SettingsFileChanged();
    }

    public void PresetSaveHandler(SerialPort_Info connection_SerialPort)
    {
        if (string.IsNullOrEmpty(_connection_SerialPort_VM.Selected_SerialPort))
        {
            _connection_SerialPort_VM.Message_PortNotFound_IsVisible = true;
        }

        _connection_SerialPort_VM.ReScan_SerialPorts(connection_SerialPort);
    }

    public SerialPort_Info GetSerialPortClientSettings()
    {
        return new SerialPort_Info()
        {
            Port = _connection_SerialPort_VM.Selected_SerialPort,
            BaudRate = _connection_SerialPort_VM.Selected_BaudRate,
            BaudRate_IsCustom = _connection_SerialPort_VM.BaudRate_IsCustom,
            BaudRate_Custom = _connection_SerialPort_VM.Custom_BaudRate_Value,
            Parity = _connection_SerialPort_VM.Selected_Parity,
            DataBits = _connection_SerialPort_VM.Selected_DataBits,
            StopBits = _connection_SerialPort_VM.Selected_StopBits
        };
    }

    public IP_Info GetIpClientSettings()
    {
        return new IP_Info()
        {
            IP_Address = _connection_Ethernet_VM.IP_Address,
            Port = _connection_Ethernet_VM.Port
        };
    }
}
