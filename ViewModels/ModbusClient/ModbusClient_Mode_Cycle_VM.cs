using ReactiveUI;
using System.Text;
using System.Reactive;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reactive.Linq;
using MessageBox.Core;
using ViewModels.Validation;
using Core.Models;
using Core.Models.Modbus.DataTypes;
using Core.Clients.DataTypes;
using Services.Interfaces;
using Core.Models.Modbus;
using MessageBusTypes.ModbusClient;

namespace ViewModels.ModbusClient;

public class ModbusClient_Mode_Cycle_VM : ValidatedDateInput, IValidationFieldInfo
{
    private bool _ui_IsEnable = false;

    public bool UI_IsEnable
    {
        get => _ui_IsEnable;
        set => this.RaiseAndSetIfChanged(ref _ui_IsEnable, value);
    }

    private bool _selectedNumberFormat_Hex;

    public bool SelectedNumberFormat_Hex
    {
        get => _selectedNumberFormat_Hex;
        set => this.RaiseAndSetIfChanged(ref _selectedNumberFormat_Hex, value);
    }

    private bool _selectedNumberFormat_Dec;

    public bool SelectedNumberFormat_Dec
    {
        get => _selectedNumberFormat_Dec;
        set => this.RaiseAndSetIfChanged(ref _selectedNumberFormat_Dec, value);
    }

    private string? _numberFormat;

    public string? NumberFormat
    {
        get => _numberFormat;
        set => this.RaiseAndSetIfChanged(ref _numberFormat, value);
    }

    private string? _slaveID;

    public string? SlaveID
    {
        get => _slaveID;
        set
        {
            this.RaiseAndSetIfChanged(ref _slaveID, value);
            ValidateInput(nameof(SlaveID), value);
        }
    }

    private bool _checkSum_IsEnable;

    public bool CheckSum_IsEnable
    {
        get => _checkSum_IsEnable;
        set => this.RaiseAndSetIfChanged(ref _checkSum_IsEnable, value);
    }

    private bool _checkSum_IsVisible;

    public bool CheckSum_IsVisible
    {
        get => _checkSum_IsVisible;
        set => this.RaiseAndSetIfChanged(ref _checkSum_IsVisible, value);
    }

    private ObservableCollection<string> _readFunctions = new ObservableCollection<string>();

    public ObservableCollection<string> ReadFunctions
    {
        get => _readFunctions;
        set => this.RaiseAndSetIfChanged(ref _readFunctions, value);
    }

    private string? _selectedReadFunction;

    public string? SelectedReadFunction
    {
        get => _selectedReadFunction;
        set => this.RaiseAndSetIfChanged(ref _selectedReadFunction, value);
    }

    private string _period_ms = "600";

    public string Period_ms
    {
        get => _period_ms;
        set
        {
            this.RaiseAndSetIfChanged(ref _period_ms, value);
            ValidateInput(nameof(Period_ms), value);
        }
    }

    private string? _address;

    public string? Address
    {
        get => _address;
        set
        {
            this.RaiseAndSetIfChanged(ref _address, value);
            ValidateInput(nameof(Address), value);
        }
    }

    private string? _numberOfRegisters;

    public string? NumberOfRegisters
    {
        get => _numberOfRegisters;
        set
        {
            this.RaiseAndSetIfChanged(ref _numberOfRegisters, value);
            ValidateInput(nameof(NumberOfRegisters), value);
        }
    }

    private const string Button_Content_Start = "Начать опрос";
    private const string Button_Content_Stop = "Остановить опрос";

    private string _button_Content = Button_Content_Start;

    public string Button_Content
    {
        get => _button_Content;
        set => this.RaiseAndSetIfChanged(ref _button_Content, value);
    }

    public bool IsStart { get; private set; } = false;

    public ReactiveCommand<Unit, Unit> Command_Start_Stop_Polling { get; }

    private NumberStyles _numberViewStyle;

    private byte _selectedSlaveID = 0;
    private ushort _selectedAddress = 0;
    private ushort _selectedNumberOfRegisters = 1;
    private uint _selectedPeriod = 600;

    // Время в мс. взято с запасом.
    // Это время нужно для совместимости с методом Receive() из класса SerialPortClient
    private const int TimeForReadHandler = 100;

    private readonly IMessageBoxMainWindow _messageBox;
    private readonly ConnectedHost _connectedHostModel;
    private readonly Model_Modbus _modbusModel;

    public ModbusClient_Mode_Cycle_VM(IMessageBoxMainWindow messageBox, ConnectedHost connectedHostModel, Model_Modbus modbusModel)
    {
        _messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));
        _connectedHostModel = connectedHostModel ?? throw new ArgumentNullException(nameof(connectedHostModel));
        _modbusModel = modbusModel ?? throw new ArgumentNullException(nameof(modbusModel));

        _connectedHostModel.DeviceIsConnect += Model_DeviceIsConnect;
        _connectedHostModel.DeviceIsDisconnected += Model_DeviceIsDisconnected;

        _modbusModel.Model_ErrorInCycleMode += Modbus_Model_ErrorInCycleMode;

        Command_Start_Stop_Polling = ReactiveCommand.Create(() =>
        {
            if (IsStart)
            {
                StopPolling();
                return;
            }

            StartPolling();
        });
        Command_Start_Stop_Polling.ThrownExceptions.Subscribe(error => _messageBox.Show(error.Message, MessageType.Error, error));

        foreach (ModbusReadFunction element in Function.AllReadFunctions)
        {
            ReadFunctions.Add(element.DisplayedName);
        }

        SelectedReadFunction = Function.ReadInputRegisters.DisplayedName;

        SelectedNumberFormat_Hex = true;

        CheckSum_IsEnable = true;
        CheckSum_IsVisible = true;

        this.WhenAnyValue(x => x.SelectedNumberFormat_Hex, x => x.SelectedNumberFormat_Dec)
            .Subscribe(values =>
            {
                try
                {
                    if (values.Item1 == true && values.Item2 == true)
                    {
                        return;
                    }

                    // Выбран шестнадцатеричный формат числа в полях Адрес и Данные
                    if (values.Item1)
                    {
                        SelectNumberFormat_Hex();
                    }

                    // Выбран десятичный формат числа в полях Адрес и Данные
                    else if (values.Item2)
                    {
                        SelectNumberFormat_Dec();
                    }
                }

                catch (Exception error)
                {
                    messageBox.Show($"Ошибка смены формата.\n\n{error.Message}", MessageType.Error, error);
                }
            });
    }

    public void Subscribe(ModbusClient_VM parent)
    {
        parent.CheckSum_VisibilityChanged += Parent_CheckSum_VisibilityChanged;
    }

    private void Parent_CheckSum_VisibilityChanged(object? sender, bool e)
    {
        CheckSum_IsVisible = e;
    }

    public string GetFieldViewName(string fieldName)
    {
        switch (fieldName)
        {
            case nameof(SlaveID):
                return "Slave ID";

            case nameof(Address):
                return "Адрес";

            case nameof(NumberOfRegisters):
                return "Кол-во регистров";

            case nameof(Period_ms):
                return "Период";

            default:
                return fieldName;
        }
    }

    public void SourceWindowClosingAction()
    {
        _modbusModel.CycleMode_Stop();
        _modbusModel.Model_ErrorInCycleMode -= Modbus_Model_ErrorInCycleMode;
    }

    private void Model_DeviceIsConnect(object? sender, IConnection? e)
    {
        UI_IsEnable = true;
    }

    private void Model_DeviceIsDisconnected(object? sender, IConnection? e)
    {
        UI_IsEnable = false;

        StopPolling();
    }

    private void Modbus_Model_ErrorInCycleMode(object? sender, Exception e)
    {
        StopPolling();

        _messageBox.Show($"Ошибка отправки команды в цикличном опросе.\n\n{e.Message}\n\nОпрос остановлен.", MessageType.Error, e);
    }

    private void SelectNumberFormat_Hex()
    {
        NumberFormat = ModbusClient_VM.ViewContent_NumberStyle_hex;
        _numberViewStyle = NumberStyles.HexNumber;

        if (!string.IsNullOrWhiteSpace(SlaveID) && string.IsNullOrEmpty(GetFullErrorMessage(nameof(SlaveID))))
        {
            SlaveID = _selectedSlaveID.ToString("X");
        }

        else
        {
            _selectedSlaveID = 0;
        }

        if (!string.IsNullOrWhiteSpace(Address) && string.IsNullOrEmpty(GetFullErrorMessage(nameof(Address))))
        {
            Address = _selectedAddress.ToString("X");
        }

        else
        {
            _selectedAddress = 0;
        }

        ValidateInput(nameof(SlaveID), SlaveID);
        ValidateInput(nameof(Address), Address);

        ChangeNumberStyleInErrors(nameof(SlaveID), NumberStyles.HexNumber);
        ChangeNumberStyleInErrors(nameof(Address), NumberStyles.HexNumber);
    }

    private void SelectNumberFormat_Dec()
    {
        NumberFormat = ModbusClient_VM.ViewContent_NumberStyle_dec;
        _numberViewStyle = NumberStyles.Number;

        if (!string.IsNullOrWhiteSpace(SlaveID) && string.IsNullOrEmpty(GetFullErrorMessage(nameof(SlaveID))))
        {
            SlaveID = int.Parse(SlaveID, NumberStyles.HexNumber).ToString();
        }

        else
        {
            _selectedSlaveID = 0;
        }

        if (!string.IsNullOrWhiteSpace(Address) && string.IsNullOrEmpty(GetFullErrorMessage(nameof(Address))))
        {
            Address = int.Parse(Address, NumberStyles.HexNumber).ToString();
        }

        else
        {
            _selectedAddress = 0;
        }

        ValidateInput(nameof(SlaveID), SlaveID);
        ValidateInput(nameof(Address), Address);

        ChangeNumberStyleInErrors(nameof(SlaveID), NumberStyles.Number);
        ChangeNumberStyleInErrors(nameof(Address), NumberStyles.Number);
    }

    private void StartPolling()
    {
        if (string.IsNullOrEmpty(SlaveID))
        {
            _messageBox.Show("Укажите Slave ID.", MessageType.Warning);
            return;
        }

        if (string.IsNullOrEmpty(Address))
        {
            _messageBox.Show("Укажите адрес Modbus регистра.", MessageType.Warning);
            return;
        }

        if (string.IsNullOrEmpty(NumberOfRegisters))
        {
            _messageBox.Show("Укажите количество регистров для чтения.", MessageType.Warning);
            return;
        }

        string? validationMessage = CheckReadFields();

        if (!string.IsNullOrEmpty(validationMessage))
        {
            _messageBox.Show(validationMessage, MessageType.Warning);
            return;
        }

        if (_selectedPeriod < _connectedHostModel.Host_ReadTimeout + TimeForReadHandler)
        {
            _messageBox.Show("Значение периода опроса не может быть меньше суммы таймаута чтения и " +
                TimeForReadHandler + " мс. (" + _connectedHostModel.Host_ReadTimeout + " мс. + " + TimeForReadHandler + "мс.)\n" +
                "Таймаут чтения: " + _connectedHostModel.Host_ReadTimeout + " мс.", MessageType.Warning);

            return;
        }

        ModbusReadFunction ReadFunction = Function.AllReadFunctions.Single(x => x.DisplayedName == SelectedReadFunction);

        _modbusModel.CycleMode_Period = _selectedPeriod;
        _modbusModel.CycleMode_Start(() =>
        {
            MessageBus.Current.SendMessage(
                new ModbusReadMessage(MainWindow_VM.SenderName, _selectedSlaveID, _selectedAddress, ReadFunction, _selectedNumberOfRegisters, CheckSum_IsEnable)
                );
        });

        Button_Content = Button_Content_Stop;
        IsStart = true;
    }

    public void StopPolling()
    {
        _modbusModel.CycleMode_Stop();

        Button_Content = Button_Content_Start;
        IsStart = false;
    }

    private string? CheckReadFields()
    {
        if (!HasErrors)
        {
            return null;
        }

        StringBuilder message = new StringBuilder();

        // Проверка полей в основном контроле

        foreach (KeyValuePair<string, ValidateMessage> element in ActualErrors)
        {
            message.AppendLine($"[{GetFieldViewName(element.Key)}]\n{GetFullErrorMessage(element.Key)}\n");
        }

        if (message.Length > 0)
        {
            message.Insert(0, "Ошибки валидации:\n\n");
            return message.ToString().TrimEnd('\r', '\n');
        }

        return null;
    }

    protected override ValidateMessage? GetErrorMessage(string fieldName, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        switch (fieldName)
        {
            case nameof(SlaveID):
                return Check_SlaveID(value);

            case nameof(Address):
                return Check_Address(value);

            case nameof(NumberOfRegisters):
                return Check_NumberOfRegisters(value);

            case nameof(Period_ms):
                return Check_Period(value);
        }

        return null;
    }

    private ValidateMessage? Check_SlaveID(string value)
    {
        if (!StringValue.IsValidNumber(value, _numberViewStyle, out _selectedSlaveID))
        {
            switch (_numberViewStyle)
            {
                case NumberStyles.Number:
                    return AllErrorMessages[DecError_Byte];

                case NumberStyles.HexNumber:
                    return AllErrorMessages[HexError_Byte];
            }
        }

        return null;
    }

    private ValidateMessage? Check_Address(string value)
    {
        if (!StringValue.IsValidNumber(value, _numberViewStyle, out _selectedAddress))
        {
            switch (_numberViewStyle)
            {
                case NumberStyles.Number:
                    return AllErrorMessages[DecError_UInt16];

                case NumberStyles.HexNumber:
                    return AllErrorMessages[HexError_UInt16];
            }
        }

        return null;
    }

    private ValidateMessage? Check_NumberOfRegisters(string value)
    {
        if (!StringValue.IsValidNumber(value, NumberStyles.Number, out _selectedNumberOfRegisters))
        {
            return AllErrorMessages[DecError_UInt16];
        }

        return null;
    }

    private ValidateMessage? Check_Period(string value)
    {
        if (!StringValue.IsValidNumber(value, NumberStyles.Number, out _selectedPeriod))
        {
            return AllErrorMessages[DecError_uint];
        }

        return null;
    }
}
