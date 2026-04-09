using Core.Clients.DataTypes;
using Core.Models;
using Core.Models.Logging;
using Core.Models.Modbus;
using Core.Models.Modbus.DataTypes;
using Core.Models.Modbus.Message;
using Core.Models.Settings;
using Core.Models.Settings.FileTypes;
using MessageBox.Core;
using MessageBusTypes.Chart;
using ReactiveUI;
using Services.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Reactive;
using System.Text;
using ViewModels.Chart;
using ViewModels.ModbusClient.Manual;
using ViewModels.Validation;

namespace ViewModels.ModbusClient.Monitoring;

public class MonitoringStateEventArgs : EventArgs
{
    public readonly bool IsRunning;

    public MonitoringStateEventArgs(bool isRunning)
    {
        IsRunning = isRunning;
    }
}

public partial class ModbusMonitoring_VM : ValidatedDateInput, IValidationFieldInfo
{
    public event EventHandler<MonitoringStateEventArgs>? ModbusMonitoringStateChanged;

    private bool ui_IsEnable;

    public bool UI_IsEnable
    {
        get => ui_IsEnable;
        set => this.RaiseAndSetIfChanged(ref ui_IsEnable, value);
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

    private bool _isMonitoringRunning;

    public bool IsMonitoringRunning
    {
        get => _isMonitoringRunning;
        set => this.RaiseAndSetIfChanged(ref _isMonitoringRunning, value);
    }

    private string? _period_ms;

    public string? Period_ms
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

    private object? _dataGrid_VM;

    public object? DataGrid_VM
    {
        get => _dataGrid_VM;
        set => this.RaiseAndSetIfChanged(ref _dataGrid_VM, value);
    }

    private bool _hasSelectedItems;

    public bool HasSelectedItems
    {
        get => _hasSelectedItems;
        set => this.RaiseAndSetIfChanged(ref _hasSelectedItems, value);
    }

    private bool _logWillStart;

    public bool LogWillStart
    {
        get => _logWillStart;
        set => this.RaiseAndSetIfChanged(ref _logWillStart, value);
    }

    private string? _startLogDate;

    public string? StartLogDate
    {
        get => _startLogDate;
        set => this.RaiseAndSetIfChanged(ref _startLogDate, value);
    }

    private bool _startLogDateIsVisible;

    public bool StartLogDateIsVisible
    {
        get => _startLogDateIsVisible;
        set => this.RaiseAndSetIfChanged(ref _startLogDateIsVisible, value);
    }

    private bool _waitStartLogMessageIsVisible;

    public bool WaitStartLogMessageIsVisible
    {
        get => _waitStartLogMessageIsVisible;
        set => this.RaiseAndSetIfChanged(ref _waitStartLogMessageIsVisible, value);
    }

    public ReactiveCommand<Unit, Unit> Command_Start_Stop_Polling { get; }
    public ReactiveCommand<Unit, Unit> Command_RemoveSelectedItems { get; }    
    public ReactiveCommand<Unit, Unit> Command_OpenChart { get; }
    public ReactiveCommand<Unit, Unit> Command_StartLogging { get; }
    public ReactiveCommand<Unit, Unit> Command_StopLogging { get; }
    public ReactiveCommand<Unit, Unit> Command_OpenLogFolder { get; }

    private NumberStyles _numberViewStyle;
    private byte _selectedSlaveID;
    private uint _selectedPeriod;

    private readonly IOpenChildWindowService _openChildWindowService;
    private readonly IFileSystemService _fileSystemService;
    private readonly IMessageBoxMainWindow _messageBox;
    private readonly Model_Settings _settingsModel;
    private readonly ConnectedHost _connectedHostModel;
    private readonly Model_Modbus _modbusModel;
    private readonly MonitoringDataGrid_VM _monitoringDataGrid_VM;
    private readonly Chart_VM _chart_VM;
    private readonly FileLogger _logger;


    public ModbusMonitoring_VM(IOpenChildWindowService openChildWindowService, IFileSystemService fileSystemService, IMessageBoxMainWindow messageBox,
        Model_Settings settingsModel, ConnectedHost connectedHostModel,
        Model_Modbus modbusModel, MonitoringDataGrid_VM monitoringDataGrid_VM, Chart_VM chart_VM,
        FileLogger logger)
    {
        _openChildWindowService = openChildWindowService ?? throw new ArgumentNullException(nameof(openChildWindowService));
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        _messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));
        _settingsModel = settingsModel ?? throw new ArgumentNullException(nameof(settingsModel));
        _connectedHostModel = connectedHostModel ?? throw new ArgumentNullException(nameof(connectedHostModel));
        _modbusModel = modbusModel ?? throw new ArgumentNullException(nameof(modbusModel));
        _monitoringDataGrid_VM = monitoringDataGrid_VM ?? throw new ArgumentNullException(nameof(monitoringDataGrid_VM));
        _chart_VM = chart_VM ?? throw new ArgumentNullException(nameof(chart_VM));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _connectedHostModel.DeviceIsConnect += Model_DeviceIsConnect;
        _connectedHostModel.DeviceIsDisconnected += Model_DeviceIsDisconnected;

        _modbusModel.Model_MonitoringError += Model_MonitoringError;

        _monitoringDataGrid_VM.PropertyChanged += MonitoringDataGrid_VM_PropertyChanged;
                
        /****************************************************/
        //
        // Первоначальная настройка UI
        //
        /****************************************************/

        foreach (ModbusReadFunction element in Function.AllReadFunctions)
        {
            ReadFunctions.Add(element.DisplayedName);
        }

        DataGrid_VM = _monitoringDataGrid_VM;

        /****************************************************/
        //
        // Настройка свойств и команд модели отображения
        //
        /****************************************************/

        Command_Start_Stop_Polling = ReactiveCommand.Create(() =>
        {
            if (IsMonitoringRunning)
            {
                StopPolling();
                return;
            }

            StartPolling();
        });
        Command_Start_Stop_Polling.ThrownExceptions.Subscribe(error => _messageBox.Show(error.Message, MessageType.Error, error));

        Command_RemoveSelectedItems = ReactiveCommand.Create(_monitoringDataGrid_VM.RemoveSelectedItems);
        Command_RemoveSelectedItems.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка удаления выбранных регистров.\n\n{error.Message}", MessageType.Error, error));

        Command_OpenChart = ReactiveCommand.Create(() =>
        {
            if (_openChildWindowService.ChartWindowIsOpen)
            {
                _openChildWindowService.RaiseChartWindow();
                return;
            }

            if (!_monitoringDataGrid_VM.GetItemsForChartAndLog().Any())
            {
                _messageBox.Show("Не выбрано ни одного регистра для отображения на графике.", MessageType.Warning);
                return;
            }

            _openChildWindowService.Chart();

            InitChartAxes();
        });
        Command_OpenChart.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка открытия окна графика.\n\n{error.Message}", MessageType.Error, error));

        Command_StartLogging = ReactiveCommand.Create(StartLogging);
        Command_StartLogging.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка запуска логгирования.\n\n{error.Message}", MessageType.Error, error));

        Command_StopLogging = ReactiveCommand.CreateFromTask(StopLogging);
        Command_StopLogging.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка остановки логгирования.\n\n{error.Message}", MessageType.Error, error));

        Command_OpenLogFolder = ReactiveCommand.CreateFromTask(async () => await _fileSystemService.OpenFolder(_settingsModel.LogFolderPath));
        Command_OpenLogFolder.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка открытия папки с логами.\n\n{error.Message}", MessageType.Error, error));

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

                    _monitoringDataGrid_VM.ChangeNumberFormat(_numberViewStyle);
                }

                catch (Exception error)
                {
                    _messageBox.Show($"Ошибка смены формата.\n\n{error.Message}", MessageType.Error, error);
                }
            });

        // Действия после запуска приложения

        SetMonitoringParameters(_settingsModel.ModbusMonitoringItems);
    }

    public void ClearData()
    {
        _monitoringDataGrid_VM.ClearData();
    }

    public void SetMonitoringParameters(ModbusMonitoringParameters data)
    {
        SlaveID = data.SlaveID.ToString();  // SlaveID приведется к нужному формату после смены RadioButton SelectedNumberFormat_Hex или SelectedNumberFormat_Dec

        var function = Function.AllFunctions.FirstOrDefault(e => e.Number == data.FunctionNumber);

        if (function != null && ReadFunctions.Any(displayedName => displayedName == function.DisplayedName))
        {
            SelectedReadFunction = function.DisplayedName;
        }

        else
        {
            SelectedReadFunction = ReadFunctions.First();
        }

        Period_ms = data.Period.ToString();

        if (data.NumberStyle == NumberStyles.HexNumber)
        {
            SelectedNumberFormat_Hex = true;
        }

        else
        {
            SelectedNumberFormat_Dec = true;
        }
    }

    public ModbusMonitoringParameters GetParametersForSave()
    {
        return new ModbusMonitoringParameters()
        {
            SlaveID = _selectedSlaveID,
            FunctionNumber = Function.AllReadFunctions.FirstOrDefault(e => e.DisplayedName == SelectedReadFunction)?.Number ?? 1,
            Period = _selectedPeriod,
            NumberStyle = _numberViewStyle,
            ChartInfo = new MonitoringChart()
            {
                NumberOfVisiblePoints = _chart_VM.NumberOfVisiblePoints,
                ChartIsTopmost = _chart_VM.WindowIsTopmost,
            },
            Items = _monitoringDataGrid_VM
                    .Items
                    .Select(e => new ModbusMonitoringItemData()
                    {
                        Address = e.SelectedAddress,
                        Alias = e.Alias,
                        ValueType = e.SelectedValueType == null ? default : e.SelectedValueType.Value,
                        VisibleOnlyRawValue = e.VisibleOnlyRawValue,
                        Formula = e.Formula,
                        ShowOnChartAndLog = e.ShowOnChartAndLog,
                    })
                    .ToList(),
        };
    }

    private void MonitoringDataGrid_VM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not MonitoringDataGrid_VM dataGrid)
            return;

        if (e.PropertyName == nameof(dataGrid.HasSelectedItems))
        {
            HasSelectedItems = dataGrid.HasSelectedItems;
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
        if (!IsMonitoringRunning)
            return;

        _messageBox.Show($"Ошибка мониторинга.\n\n{e.Message}", MessageType.Error, e);

        StopPolling();
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

    #region Логгирование

    private void StartLogging()
    {
        if (_logger.IsRunning)
            return;

        if (!_monitoringDataGrid_VM.GetItemsForChartAndLog().Any())
        {
            _messageBox.Show("Не выбрано ни одного регистра для записи в лог.", MessageType.Warning);
            ResetLogFlags();
            return;
        }

        LogWillStart = true;

        if (!IsMonitoringRunning)
        {
            WaitStartLogMessageIsVisible = true;
            return;
        }

        var startDate = DateTime.Now;
        var filePath = Path.Combine(_settingsModel.LogFolderPath, $"CoreBus {startDate:dd.MM.yyyy HH.mm.ss}.txt");

        var columnNames = new StringBuilder();

        foreach (var item in _monitoringDataGrid_VM.Items)
        {
            if (item.ItemShowOnChartAndLog)
                columnNames.Append($"{item.GetDisplayedItemName()}\t");
        }

        _logger.Start(filePath, columnNames.ToString(), _settingsModel.Settings?.LogTimestampFormat ?? DeviceData.LogTimestampFormat_Default);

        WaitStartLogMessageIsVisible = false;

        StartLogDateIsVisible = true;
        StartLogDate = startDate.ToString("dd.MM.yyyy HH:mm:ss");
    }

    private async Task StopLogging()
    {
        await _logger.StopAsync();

        ResetLogFlags();
    }

    private void ResetLogFlags()
    {
        LogWillStart = false;

        WaitStartLogMessageIsVisible = false;
        StartLogDateIsVisible = false;
    }

    #endregion Логгирование

    #region Опрос

    private void StartPolling()
    {
        try
        {
            if (_openChildWindowService.MacrosWindowIsOpen)
            {
                _messageBox.Show("Для запуска опроса регистров необходимо закрыть окно макросов.", MessageType.Warning);
                return;
            }

            if (_monitoringDataGrid_VM.IsEmpty)
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

            if (_selectedPeriod == 0)
            {
                _messageBox.Show("Вы не ошиблись с периодом? :)", MessageType.Warning);
                return;
            }

            Button_Content = Button_Content_Stop;
            IsMonitoringRunning = true;

            _monitoringDataGrid_VM.BlockUI(true);

            if (_openChildWindowService.ChartWindowIsOpen)
                InitChartAxes();

            MessageBus.Current.SendMessage(new ManageChartToolsMessage(false));

            if (WaitStartLogMessageIsVisible)
                StartLogging();

            _modbusModel.MonitoringStart(MonitoringRequestAction, (int)_selectedPeriod);

            ModbusMonitoringStateChanged?.Invoke(this, new MonitoringStateEventArgs(isRunning: true));
        }
        
        catch (Exception)
        {
            StopPolling();
            throw;
        }
    }

    private void InitChartAxes()
    {
        var chartAxes = _monitoringDataGrid_VM.GetItemsForChartAndLog().ToDictionary(e => e.Id, e => e.GetDisplayedItemName());

        if (chartAxes.Count != 0)
        {
            MessageBus.Current.SendMessage(new InitAxesMessage(chartAxes, _selectedPeriod));
        }
    }

    public void StopPolling()
    {
        _modbusModel.MonitoringStop();

        Button_Content = Button_Content_Start;
        IsMonitoringRunning = false;

        _monitoringDataGrid_VM.BlockUI(false);

        MessageBus.Current.SendMessage(new ManageChartToolsMessage(true));

        Task.Run(StopLogging);

        ModbusMonitoringStateChanged?.Invoke(this, new MonitoringStateEventArgs(isRunning: false));
    }

    private async Task MonitoringRequestAction()
    {
        try
        {
            if (_connectedHostModel.DisconnectedByClient)
                return;

            if (_connectedHostModel.HostIsConnect == false)
                throw new Exception("Клиент отключен.");

            if (ModbusClient_VM.ModbusMessageType == null)
                throw new Exception("Не задан тип протокола Modbus.");

            var allAddresses = _monitoringDataGrid_VM.Items.Select(e => e.SelectedAddress);

            ushort startingAddress = allAddresses.Min();
            int numberOfRegisters = allAddresses.Max() - allAddresses.Min() + 1;

            ModbusReadFunction readFunction = Function.AllReadFunctions.Single(x => x.DisplayedName == SelectedReadFunction);

            MessageData data = new ReadTypeMessage(
                _selectedSlaveID,
                startingAddress,
                numberOfRegisters,
                ModbusClient_VM.ModbusMessageType is ModbusTCP_Message ? false : true);

            ModbusOperationResult result = await _modbusModel.ReadRegister(
                            readFunction,
                            data,
                            ModbusClient_VM.ModbusMessageType);

            if (result.ReadedData == null || !IsMonitoringRunning)
                return;

            var logString = _monitoringDataGrid_VM.DisplayData(result.ReadedData, readFunction, startingAddress, numberOfRegisters, _selectedPeriod);

            if (_logger.IsRunning)
                _logger.WriteLine(logString);
        }
        
        catch (Exception)
        {
            if (!_connectedHostModel.DisconnectedByClient)
                throw;
        }
    }

    #endregion Опрос
}
