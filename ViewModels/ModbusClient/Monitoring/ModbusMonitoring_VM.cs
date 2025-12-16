using Core.Clients.DataTypes;
using Core.Models;
using Core.Models.Modbus;
using Core.Models.Modbus.DataTypes;
using Core.Models.Modbus.Message;
using MessageBox.Core;
using ReactiveUI;
using Services.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Reactive;
using System.Text;
using ViewModels.ModbusClient.Manual;
using ViewModels.Validation;

namespace ViewModels.ModbusClient.Monitoring;

public class ModbusMonitoring_VM : ValidatedDateInput, IValidationFieldInfo
{
    private bool ui_IsEnable = false;

    public bool UI_IsEnable
    {
        get => ui_IsEnable;
        set => this.RaiseAndSetIfChanged(ref ui_IsEnable, value);
    }

    private string _slaveID = "7";

    public string SlaveID
    {
        get => _slaveID;
        set
        {
            this.RaiseAndSetIfChanged(ref _slaveID, value);
            ValidateInput(nameof(SlaveID), value);
        }
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

    private bool _isStart = false;

    public bool IsStart
    {
        get => _isStart;
        set => this.RaiseAndSetIfChanged(ref _isStart, value);
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

    private const string Button_Content_Start = "Начать опрос";
    private const string Button_Content_Stop = "Остановить опрос";

    private string _button_Content = Button_Content_Start;

    public string Button_Content
    {
        get => _button_Content;
        set => this.RaiseAndSetIfChanged(ref _button_Content, value);
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

    private bool _hasSelectedItems;

    public bool HasSelectedItems
    {
        get => _hasSelectedItems;
        set => this.RaiseAndSetIfChanged(ref _hasSelectedItems, value);
    }

    private bool _allRowSelected;

    public bool AllRowSelected
    {
        get => _allRowSelected;
        set => this.RaiseAndSetIfChanged(ref _allRowSelected, value);
    }

    private ObservableCollection<MonitoringItem_VM> _monitoringItems = new ObservableCollection<MonitoringItem_VM>();

    public ObservableCollection<MonitoringItem_VM> MonitoringItems
    {
        get => _monitoringItems;
        set => this.RaiseAndSetIfChanged(ref _monitoringItems, value);
    }

    public ReactiveCommand<Unit, Unit> Command_Start_Stop_Polling { get; }
    public ReactiveCommand<Unit, Unit> Command_RemoveSelectedItems { get; }
    public ReactiveCommand<Unit, Unit> Command_SelectAllRows { get; }
    public ReactiveCommand<Unit, Unit> Command_AddRegister { get; }

    private NumberStyles _numberViewStyle;

    private byte _selectedSlaveID = 0;

    private readonly IMessageBoxMainWindow _messageBox;
    private readonly ConnectedHost _connectedHostModel;
    private readonly Model_Modbus _modbusModel;
        

    public ModbusMonitoring_VM(IMessageBoxMainWindow messageBox, ConnectedHost connectedHostModel, Model_Modbus modbusModel)
    {
        _messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));
        _connectedHostModel = connectedHostModel ?? throw new ArgumentNullException(nameof(connectedHostModel));
        _modbusModel = modbusModel ?? throw new ArgumentNullException(nameof(modbusModel));

        _connectedHostModel.DeviceIsConnect += Model_DeviceIsConnect;
        _connectedHostModel.DeviceIsDisconnected += Model_DeviceIsDisconnected;

        _modbusModel.Model_MonitoringError += Model_MonitoringError;

        /****************************************************/
        //
        // Первоначальная настройка UI
        //
        /****************************************************/

        SelectedNumberFormat_Dec = true;

        foreach (ModbusReadFunction element in Function.AllReadFunctions)
        {
            ReadFunctions.Add(element.DisplayedName);
        }

        SelectedReadFunction = Function.ReadInputRegisters.DisplayedName;

        /****************************************************/
        //
        // Настройка свойств и команд модели отображения
        //
        /****************************************************/

        Command_Start_Stop_Polling = ReactiveCommand.Create(() =>
        {
            if (IsStart)
            {
                StopPolling();
                return;
            }

            StartPolling();
        });
        Command_Start_Stop_Polling.ThrownExceptions.Subscribe(error => messageBox.Show(error.Message, MessageType.Error, error));

        Command_RemoveSelectedItems = ReactiveCommand.Create(() =>
        {
            for (int i = MonitoringItems.Count - 1; i >= 0; i--)
            {
                if (MonitoringItems[i].IsSelected)
                {
                    MonitoringItems[i].PropertyChanged -= MonitoringItemOnPropertyChanged;
                    MonitoringItems.RemoveAt(i);
                }
            }

            HasSelectedItems = false;

            if (MonitoringItems.Count == 0)
                AllRowSelected = false;
        });
        Command_RemoveSelectedItems.ThrownExceptions.Subscribe(error => messageBox.Show($"Ошибка удаления выбранных регистров.\n\n{error.Message}", MessageType.Error, error));

        Command_SelectAllRows = ReactiveCommand.Create(() =>
        {
            foreach (var item in MonitoringItems)
            {
                item.PropertyChanged -= MonitoringItemOnPropertyChanged;
                item.IsSelected = AllRowSelected;
                HasSelectedItems = AllRowSelected;
                item.PropertyChanged += MonitoringItemOnPropertyChanged;
            }
        });
        Command_SelectAllRows.ThrownExceptions.Subscribe(error => messageBox.Show($"Ошибка выбора всех регистров.\n\n{error.Message}", MessageType.Error, error));

        Command_AddRegister = ReactiveCommand.Create(() =>
        {
            var initAddress = MonitoringItems.Any() && StringValue.IsValidNumber(MonitoringItems.Last().Address, _numberViewStyle, out UInt16 init) ? init + 1 : 0;

            var newItem = new MonitoringItem_VM(initAddress, _numberViewStyle, _messageBox, HiddenNotUsedRegisters);
            newItem.PropertyChanged += MonitoringItemOnPropertyChanged;

            MonitoringItems.Add(newItem);
        });
        Command_AddRegister.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка добавления регистра.\n\n{error.Message}", MessageType.Error, error));

        this.WhenAnyValue(x => x.SelectedNumberFormat_Hex, x => x.SelectedNumberFormat_Dec)
            .Subscribe(values =>
            {
                try
                {
                    var (hexSelected, decSelected) = values;

                    // Оба выбраны или оба сняты — ничего не делаем
                    if (hexSelected == decSelected)
                        return;

                    NumberFormat = hexSelected ? 
                        ModbusManualMode_VM.ViewContent_NumberStyle_hex : 
                        ModbusManualMode_VM.ViewContent_NumberStyle_dec;

                    _numberViewStyle = hexSelected ? 
                        NumberStyles.HexNumber : 
                        NumberStyles.Number;

                    ChangeNumberFormat(_numberViewStyle);

                    foreach (var item in MonitoringItems)
                    {
                        item.SetNumberFormat(_numberViewStyle);
                    }
                }

                catch (Exception error)
                {
                    messageBox.Show($"Ошибка смены формата.\n\n{error.Message}", MessageType.Error, error);
                }
            });
    }

    public void ClearData()
    {
        foreach (var item in MonitoringItems)
        {
            item.Clear();
        }
    }

    private void HiddenNotUsedRegisters()
    {
        foreach (var item in MonitoringItems)
        {
            
        }
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

    private void Model_MonitoringError(object? sender, Exception e)
    {
        _messageBox.Show($"Ошибка мониторинга.\n\n{e.Message}", MessageType.Error, e);

        StopPolling();
    }

    private void MonitoringItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MonitoringItem_VM.IsSelected))
        {
            AllRowSelected = MonitoringItems.Count > 0 &&
                             MonitoringItems.All(x => x.IsSelected);
        }

        foreach (var item in MonitoringItems)
        {
            if (item.IsSelected)
            {
                HasSelectedItems = true;
                return;
            }
        }

        HasSelectedItems = false;
    }

    private void ChangeNumberFormat(NumberStyles newStyle)
    {
        if (!string.IsNullOrWhiteSpace(SlaveID) && string.IsNullOrEmpty(GetFullErrorMessage(nameof(SlaveID))))
        {
            SlaveID = newStyle == NumberStyles.HexNumber ? _selectedSlaveID.ToString("X") : _selectedSlaveID.ToString();
        }

        else
        {
            _selectedSlaveID = 0;
        }

        ValidateInput(nameof(SlaveID), SlaveID);
        ChangeNumberStyleInErrors(nameof(SlaveID), newStyle);
    }

    private void StartPolling()
    {
        try
        {
            if (!MonitoringItems.Any())
            {
                _messageBox.Show("Не заданы регистры для опроса.", MessageType.Warning);
                return;
            }

            string? validationMessage = CheckFields();

            if (!string.IsNullOrEmpty(validationMessage))
            {
                _messageBox.Show(validationMessage, MessageType.Warning);
                return;
            }

            Button_Content = Button_Content_Stop;
            IsStart = true;

            foreach (var item in MonitoringItems)
            {
                item.UI_IsEnable = false;
            }

            _modbusModel.MonitoringStart(MonitoringRequestAction, int.Parse(Period_ms));
        }
        
        catch (Exception)
        {
            StopPolling();
            throw;
        }
    }

    public void StopPolling()
    {
        _modbusModel.MonitoringStop();

        Button_Content = Button_Content_Start;
        IsStart = false;

        foreach (var item in MonitoringItems)
        {
            item.UI_IsEnable = true;
        }
    }

    private async Task MonitoringRequestAction()
    {
        if (_connectedHostModel.HostIsConnect == false)
        {
            throw new Exception("Клиент отключен.");
        }

        if (ModbusClient_VM.ModbusMessageType == null)
        {
            throw new Exception("Не задан тип протокола Modbus.");
        }

        byte slaveID = byte.Parse(SlaveID);

        var allAddresses = MonitoringItems.Select(e => e.SelectedAddress);

        ushort startingAddress = allAddresses.Min();
        int numberOfRegisters = allAddresses.Max() - allAddresses.Min() + 1;

        ModbusReadFunction readFunction = Function.ReadInputRegisters;

        MessageData data = new ReadTypeMessage(
            slaveID,
            startingAddress,
            numberOfRegisters,
            ModbusClient_VM.ModbusMessageType is ModbusTCP_Message ? false : true);

        ModbusOperationResult result = await _modbusModel.ReadRegister(
                        readFunction,
                        data,
                        ModbusClient_VM.ModbusMessageType);

        if (result.ReadedData == null)
            return;

        var resultList = ConvertToResultList(result.ReadedData, numberOfRegisters, readFunction);

        foreach (var item in MonitoringItems)
        {
            var itemAddress = item.SelectedAddress;

            if (itemAddress >= 0 && itemAddress < resultList.Count)
            {
                item.SetReadedValue(resultList[itemAddress]);
            }
        }
    }

    private List<UInt16> ConvertToResultList(byte[] modbusData, int numberOfRegisters, ModbusReadFunction function)
    {
        if (function.Number == Function.ReadCoilStatus.Number ||
            function.Number == Function.ReadDiscreteInputs.Number)
        {
            return GetResultFromBytes(modbusData, numberOfRegisters);
        }

        return GetResultFromWords(modbusData);
    }

    private static List<UInt16> GetResultFromBytes(byte[] modbusData, int numberOfRegisters)
    {
        var result = new List<UInt16>();

        int registerCounter = 0;

        foreach (byte element in modbusData)
        {
            for (int i = 0; i < 8; i++)
            {
                if (registerCounter == numberOfRegisters) break;

                result.Add((UInt16)((element & (1 << (i))) != 0 ? 1 : 0));
                registerCounter++;
            }
        }

        return result;
    }

    private static List<UInt16> GetResultFromWords(byte[] modbusData)
    {
        var result = new List<UInt16>();

        for (int i = 0; i < modbusData.Length - 1; i += 2)
        {
            result.Add((UInt16)((modbusData[i + 1] << 8) | modbusData[i]));
        }

        // Обработка последнего байта, если длина массива нечетная
        if (modbusData.Length % 2 != 0)
        {
            result.Add(modbusData.Last());
        }

        return result;
    }

    private string? CheckFields()
    {
        var validationMessages = new StringBuilder();

        // Проверка полей в настройках мониторинга
        foreach (KeyValuePair<string, ValidateMessage> element in ActualErrors)
        {
            validationMessages.AppendLine($"[{GetFieldViewName(element.Key)}]\n{GetFullErrorMessage(element.Key)}\n");
        }

        // Проверка полей в таблице
        for (int i = 0; i < MonitoringItems.Count; i++)
        {
            var checkedItem = MonitoringItems[i];

            foreach (KeyValuePair<string, ValidateMessage> itemElement in checkedItem.ActualErrors)
            {
                validationMessages.AppendLine($"[Элемент {i + 1} - {checkedItem.GetFieldViewName(itemElement.Key)}]\n{checkedItem.GetFullErrorMessage(itemElement.Key)}\n");
            }
        }

        if (validationMessages.Length > 0)
        {
            validationMessages.Insert(0, "Ошибки валидации:\n\n");
            return validationMessages.ToString().TrimEnd('\r', '\n');
        }

        return null;
    }

    public string GetFieldViewName(string fieldName)
    {
        switch (fieldName)
        {
            case nameof(SlaveID):
                return "Slave ID";

            case nameof(Period_ms):
                return "Период";

            default:
                return fieldName;
        }
    }

    protected override ValidateMessage? GetErrorMessage(string fieldName, string? value)
    {
        switch (fieldName)
        {
            case nameof(SlaveID):
                return Check_SlaveID(value);

            case nameof(Period_ms):
                return Check_Period(value);
        }

        return null;
    }

    private ValidateMessage? Check_SlaveID(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return AllErrorMessages[NotEmptyField];
        }

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

    private ValidateMessage? Check_Period(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return AllErrorMessages[NotEmptyField];
        }

        if (!StringValue.IsValidNumber(value, NumberStyles.Number, out uint _))
        {
            return AllErrorMessages[DecError_uint];
        }

        return null;
    }
}
