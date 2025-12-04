using Core.Clients;
using Core.Clients.DataTypes;
using Core.Models;
using Core.Models.Modbus.Message;
using MessageBox.Core;
using ReactiveUI;
using Services.Interfaces;
using System.Collections.ObjectModel;
using System.Reactive;
using ViewModels.ModbusClient.Manual;
using ViewModels.ModbusClient.Monitoring;

namespace ViewModels.ModbusClient;

public class ModbusClient_VM : ReactiveObject
{
    private bool ui_IsEnable = false;

    public bool UI_IsEnable
    {
        get => ui_IsEnable;
        set => this.RaiseAndSetIfChanged(ref ui_IsEnable, value);
    }

    private const string Modbus_TCP_Name = "Modbus TCP";
    public const string Modbus_RTU_Name = "Modbus RTU";
    public const string Modbus_ASCII_Name = "Modbus ASCII";
    private const string Modbus_RTU_over_TCP_Name = "Modbus RTU over TCP";
    private const string Modbus_ASCII_over_TCP_Name = "Modbus ASCII over TCP";

    private readonly ObservableCollection<string> _modbusTypes_SerialPortClient =
        new ObservableCollection<string>()
        {
                Modbus_RTU_Name, Modbus_ASCII_Name
        };

    private readonly ObservableCollection<string> _modbusTypes_IPClient =
        new ObservableCollection<string>()
        {
                Modbus_TCP_Name, Modbus_RTU_over_TCP_Name, Modbus_ASCII_over_TCP_Name
        };

    private ObservableCollection<string>? _availableModbusTypes;

    public ObservableCollection<string>? AvailableModbusTypes
    {
        get => _availableModbusTypes;
        set => this.RaiseAndSetIfChanged(ref _availableModbusTypes, value);
    }

    private string _selectedModbusType = string.Empty;

    public string SelectedModbusType
    {
        get => _selectedModbusType;
        set => this.RaiseAndSetIfChanged(ref _selectedModbusType, value);
    }

    private bool _buttonModbusScanner_IsVisible = true;

    public bool ButtonModbusScanner_IsVisible
    {
        get => _buttonModbusScanner_IsVisible;
        set => this.RaiseAndSetIfChanged(ref _buttonModbusScanner_IsVisible, value);
    }

    private bool _buttonClearData_IsVisible;

    public bool ButtonClearData_IsVisible
    {
        get => _buttonClearData_IsVisible;
        set => this.RaiseAndSetIfChanged(ref _buttonClearData_IsVisible, value);
    }

    private bool _isMonitoringMode = false;

    public bool IsMonitoringMode
    {
        get => _isMonitoringMode;
        set => this.RaiseAndSetIfChanged(ref _isMonitoringMode, value);
    }

    private object? _currentModeViewModel;

    public object? CurrentModeViewModel
    {
        get => _currentModeViewModel;
        set => this.RaiseAndSetIfChanged(ref _currentModeViewModel, value);
    }

    public ReactiveCommand<Unit, Unit> Command_Open_ModbusScanner { get; }
    public ReactiveCommand<Unit, Unit> Command_ClearData { get; }

    public static ModbusMessage? ModbusMessageType { get; private set; }

    private readonly IOpenChildWindowService _openChildWindow;
    private readonly IMessageBoxMainWindow _messageBox;
    private readonly ConnectedHost _connectedHostModel;
    private readonly ModbusManualMode_VM _modbusManualMode_VM;
    private readonly ModbusMonitoring_VM _modbusMonitoring_VM;

    public ModbusClient_VM(IOpenChildWindowService openChildWindow, IMessageBoxMainWindow messageBox, ConnectedHost connectedHostModel, ModbusManualMode_VM modbusManualMode_VM, ModbusMonitoring_VM modbusMonitoring_VM)
    {
        _openChildWindow = openChildWindow ?? throw new ArgumentNullException(nameof(openChildWindow));
        _messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));
        _connectedHostModel = connectedHostModel ?? throw new ArgumentNullException(nameof(connectedHostModel));
        _modbusManualMode_VM = modbusManualMode_VM ?? throw new ArgumentNullException(nameof(modbusManualMode_VM));
        _modbusMonitoring_VM = modbusMonitoring_VM ?? throw new ArgumentNullException(nameof(modbusMonitoring_VM));

        _connectedHostModel.DeviceIsConnect += Model_DeviceIsConnect;
        _connectedHostModel.DeviceIsDisconnected += Model_DeviceIsDisconnected;

        Command_Open_ModbusScanner = ReactiveCommand.CreateFromTask(_openChildWindow.ModbusScanner);

        Command_ClearData = ReactiveCommand.Create(() =>
        {
            if (CurrentModeViewModel is ModbusManualMode_VM manualMode)
            {
                manualMode.ClearData();
            }
        });
        Command_ClearData.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка очистки данных.\n\n{error.Message}", MessageType.Error, error));

        this.WhenAnyValue(x => x.SelectedModbusType)
            .WhereNotNull()
            .Subscribe(x =>
            {
                if (_connectedHostModel.HostIsConnect)
                {
                    _modbusManualMode_VM.SetCheckSumVisiblity();

                    if (SelectedModbusType == Modbus_TCP_Name)
                    {
                        ModbusMessageType = new ModbusTCP_Message();
                        return;
                    }

                    if (SelectedModbusType == Modbus_RTU_Name ||
                        SelectedModbusType == Modbus_RTU_over_TCP_Name)
                    {
                        ModbusMessageType = new ModbusRTU_Message();
                        return;
                    }

                    if (SelectedModbusType == Modbus_ASCII_Name ||
                        SelectedModbusType == Modbus_ASCII_over_TCP_Name)
                    {
                        ModbusMessageType = new ModbusASCII_Message();
                        return;
                    }

                    _messageBox.Show($"Задан неизвестный тип Modbus протокола: {SelectedModbusType}", MessageType.Warning);
                }
            });

        this.WhenAnyValue(x => x.IsMonitoringMode)
            .Subscribe(_ =>
            {
                //if (!IsMonitoringMode)
                //{
                //    _cycleMode_VM.StopPolling();
                //}

                ButtonClearData_IsVisible = !IsMonitoringMode;

                CurrentModeViewModel = IsMonitoringMode ? _modbusMonitoring_VM : _modbusManualMode_VM;              
            });
    }

    private void Model_DeviceIsConnect(object? sender, IConnection? e)
    {
        if (e is IPClient)
        {
            AvailableModbusTypes = _modbusTypes_IPClient;

            ButtonModbusScanner_IsVisible = false;
        }

        else if (e is SerialPortClient)
        {
            AvailableModbusTypes = _modbusTypes_SerialPortClient;

            ButtonModbusScanner_IsVisible = true;
        }

        else
        {
            _messageBox.Show("Задан неизвестный тип подключения.", MessageType.Warning);
            return;
        }

        SelectedModbusType = AvailableModbusTypes.Contains(SelectedModbusType) ? SelectedModbusType : AvailableModbusTypes.First();

        UI_IsEnable = true;
    }

    private void Model_DeviceIsDisconnected(object? sender, IConnection? e)
    {
        UI_IsEnable = false;

        ButtonModbusScanner_IsVisible = true;
    }
}
