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

public partial class ModbusMonitoring_VM : ValidatedDateInput, IValidationFieldInfo
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
}
