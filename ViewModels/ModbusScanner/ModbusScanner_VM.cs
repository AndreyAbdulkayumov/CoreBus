using Core.Models;
using Core.Models.Modbus;
using Core.Models.Modbus.DataTypes;
using Core.Models.Modbus.Message;
using DynamicData;
using MessageBox.Core;
using ReactiveUI;
using Services.Interfaces;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using ViewModels.ModbusClient;
using ViewModels.Validation;

namespace ViewModels.ModbusScanner;

public class ModbusScanner_VM : ValidatedDateInput, IValidationFieldInfo
{
    private bool _searchInProcess = false;

    public bool SearchInProcess
    {
        get => _searchInProcess;
        set => this.RaiseAndSetIfChanged(ref _searchInProcess, value);
    }

    private string _slavesAddresses = string.Empty;

    public string SlavesAddresses
    {
        get => _slavesAddresses;
        set => this.RaiseAndSetIfChanged(ref _slavesAddresses, value);
    }

    private string _selectedModbusType = string.Empty;

    public string SelectedModbusType
    {
        get => _selectedModbusType;
        set => this.RaiseAndSetIfChanged(ref _selectedModbusType, value);
    }

    private ObservableCollection<string> _availableModbusTypes = new ObservableCollection<string>()
    {
        ModbusClient_VM.Modbus_RTU_Name, ModbusClient_VM.Modbus_ASCII_Name
    };

    public ObservableCollection<string>? AvailableModbusTypes
    {
        get => _availableModbusTypes;
    }

    private string _searchRequest = string.Empty;

    public string SearchRequest
    {
        get => _searchRequest;
        set => this.RaiseAndSetIfChanged(ref _searchRequest, value);
    }

    private string _selectedFunction = string.Empty;

    public string SelectedFunction
    {
        get => _selectedFunction;
        set => this.RaiseAndSetIfChanged(ref _selectedFunction, value);
    }

    private ObservableCollection<string> _availableFunctions = new ObservableCollection<string>();

    public ObservableCollection<string>? AvailableFunctions
    {
        get => _availableFunctions;
    }

    private string? _pauseBetweenRequests;

    public string? PauseBetweenRequests
    {
        get => _pauseBetweenRequests;
        set
        {
            this.RaiseAndSetIfChanged(ref _pauseBetweenRequests, value);
            ValidateInput(nameof(PauseBetweenRequests), value);
        }
    }

    private int _deviceReadTimeoutValue;

    public string DeviceReadTimeout => _localization.Get("Scanner.ReadTimeoutPrefix") + _deviceReadTimeoutValue + " " + _localization.Get("Common.Ms");

    private const string ButtonContent_Start_Key = "Status.StartSearch";
    private const string ButtonContent_Stop_Key = "Status.StopSearch";

    private string _actionButtonContentKey = ButtonContent_Start_Key;

    public string ActionButtonContent => _localization.Get(_actionButtonContentKey);

    private string _currentSlaveID = string.Empty;

    public string CurrentSlaveID
    {
        get => _currentSlaveID;
        set => this.RaiseAndSetIfChanged(ref _currentSlaveID, value);
    }

    private int _progressBar_Value;

    public int ProgressBar_Value
    {
        get => _progressBar_Value;
        set => this.RaiseAndSetIfChanged(ref _progressBar_Value, value);
    }

    // Минимальное значение адреса устройства Modbus.
    // Не берем в учет широковещательный адрес (SlaveId = 0).
    public int ProgressBar_Minimum
    {
        get => 1;
    }

    // Максимальное значение адреса устройства Modbus.
    public int ProgressBar_Maximum
    {
        get => 255;
    }

    public string ErrorMessageInUI => _localization.Get("Scanner.NoDeviceResponded");

    private bool _errorIsVisible = false;

    public bool ErrorIsVisible
    {
        get => _errorIsVisible;
        set => this.RaiseAndSetIfChanged(ref _errorIsVisible, value);
    }

    public ReactiveCommand<Unit, Unit> Command_Start_Stop_Search { get; }

    private readonly IMessageBox _messageBox;
    private readonly ConnectedHost _connectedHostModel;
    private readonly Model_Modbus _modbusModel;
    private readonly ILocalizationService _localization;

    private ModbusMessage _messageType;

    private Task? _searchTask;
    private CancellationTokenSource? _searchCancel;

    private uint _pauseBetweenRequests_ForWork;

    private record SearchFunctionType(string DisplayedName, string DisplayedBytes, ModbusReadFunction ReadFunction, ReadTypeMessage Message);

    private static SearchFunctionType Func_01 = new SearchFunctionType(
        DisplayedName: $"0x{Function.ReadCoilStatus.Number.ToString("X2")}",
        DisplayedBytes: "01 00 01 00 02",
        ReadFunction: Function.ReadCoilStatus,
        Message: new ReadTypeMessage(
                slaveID: 0,
                address: 0,
                numberOfRegisters: 2,
                checkSum_IsEnable: true)
        );

    private static SearchFunctionType Func_02 = new SearchFunctionType(
        DisplayedName: $"0x{Function.ReadDiscreteInputs.Number.ToString("X2")}",
        DisplayedBytes: "02 00 01 00 02",
        ReadFunction: Function.ReadDiscreteInputs,
        Message: new ReadTypeMessage(
                slaveID: 0,
                address: 0,
                numberOfRegisters: 2,
                checkSum_IsEnable: true)
        );

    private static SearchFunctionType Func_03 = new SearchFunctionType(
        DisplayedName: $"0x{Function.ReadHoldingRegisters.Number.ToString("X2")}",
        DisplayedBytes: "03 00 01 00 02",
        ReadFunction: Function.ReadHoldingRegisters,
        Message: new ReadTypeMessage(
                slaveID: 0,
                address: 0,
                numberOfRegisters: 2,
                checkSum_IsEnable: true)
        );

    private static SearchFunctionType Func_04 = new SearchFunctionType(
        DisplayedName: $"0x{Function.ReadInputRegisters.Number.ToString("X2")}",
        DisplayedBytes: "04 00 01 00 02",
        ReadFunction: Function.ReadInputRegisters,
        Message: new ReadTypeMessage(
                slaveID: 0,
                address: 0,
                numberOfRegisters: 2,
                checkSum_IsEnable: true)
        );

    private readonly SearchFunctionType[] AllSearchFunctions = new SearchFunctionType[]
    {
        Func_01, Func_02, Func_03, Func_04
    };

    public ModbusScanner_VM(IMessageBoxModbusScanner messageBox,
        ConnectedHost connectedHostModel, Model_Modbus modbusModel, ILocalizationService localization)
    {
        _messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));
        _connectedHostModel = connectedHostModel ?? throw new ArgumentNullException(nameof(connectedHostModel));
        _modbusModel = modbusModel ?? throw new ArgumentNullException(nameof(modbusModel));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _localization.LanguageChanged += (_, _) =>
        {
            this.RaisePropertyChanged(nameof(ActionButtonContent));
            this.RaisePropertyChanged(nameof(DeviceReadTimeout));
            this.RaisePropertyChanged(nameof(ErrorMessageInUI));
        };

        _messageType = ModbusClient_VM.ModbusMessageType ?? throw new ArgumentNullException("ModbusClient_VM.ModbusMessageType");

        _availableFunctions.AddRange(AllSearchFunctions.Select(e => e.DisplayedName));

        SelectedFunction = Func_03.DisplayedName;

        SelectedModbusType = _messageType.ProtocolName;

        _deviceReadTimeoutValue = _connectedHostModel.Host_ReadTimeout;
        this.RaisePropertyChanged(nameof(DeviceReadTimeout));

        Command_Start_Stop_Search = ReactiveCommand.CreateFromTask(async () =>
        {
            if (SearchInProcess)
            {
                await StopPolling();

                return;
            }

            StartPolling();
        });
        Command_Start_Stop_Search.ThrownExceptions.Subscribe(error => _messageBox.Show(error.Message, MessageType.Error, error));

        this.WhenAnyValue(x => x.SelectedFunction)
            .WhereNotNull()
            .Subscribe(x =>
            {
                var function = AllSearchFunctions.FirstOrDefault(e => e.DisplayedName == x);

                if (function != null) 
                    SearchRequest = function.DisplayedBytes;
            });

        this.WhenAnyValue(x => x.SelectedModbusType)
            .WhereNotNull()
            .Subscribe(x =>
            {
                if (!_connectedHostModel.HostIsConnect)
                    return;

                switch (SelectedModbusType)
                {
                    case ModbusClient_VM.Modbus_RTU_Name:
                        _messageType = new ModbusRTU_Message();
                        break;

                    case ModbusClient_VM.Modbus_ASCII_Name:
                        _messageType = new ModbusASCII_Message();
                        break;

                    default:
                        _messageBox.Show(_localization.Get("Warning.UnknownModbusTypeMultiline", SelectedModbusType ?? string.Empty), MessageType.Warning);
                        break;
                }
            });

        // Значения по умолчанию
        PauseBetweenRequests = "200";
    }

    public async Task WindowClosed()
    {
        if (SearchInProcess && _searchTask != null)
        {
            _searchCancel?.Cancel();

            await Task.WhenAny(_searchTask);
        }
    }

    private void StartPolling()
    {
        if (string.IsNullOrEmpty(PauseBetweenRequests))
        {
            _messageBox.Show(_localization.Get("Warning.PauseNotSet"), MessageType.Warning);
            return;
        }

        string? validationMessage = CheckFields();

        if (!string.IsNullOrEmpty(validationMessage))
        {
            _messageBox.Show(validationMessage, MessageType.Warning);
            return;
        }

        _actionButtonContentKey = ButtonContent_Stop_Key;
        this.RaisePropertyChanged(nameof(ActionButtonContent));
        SearchInProcess = true;

        SlavesAddresses = string.Empty;

        ErrorIsVisible = false;

        _searchCancel = new CancellationTokenSource();

        _searchTask = Task.Run(() => SearchDevices(_searchCancel.Token));
    }

    private void ViewSlaveAddress(int slaveId)
    {
        SlavesAddresses +=
            "Slave ID:\n" +
            "dec:   " + slaveId + "\n" +
            "hex:   " + slaveId.ToString("X2") + "\n\n";
    }

    private string? CheckFields()
    {
        if (!HasErrors)
        {
            return null;
        }

        StringBuilder message = new StringBuilder();

        foreach (KeyValuePair<string, ValidateMessage> element in ActualErrors)
        {
            message.AppendLine($"[{GetFieldViewName(element.Key)}]\n{GetFullErrorMessage(element.Key)}\n");
        }

        if (message.Length > 0)
        {
            message.Insert(0, _localization.Get("Validation.ErrorsHeader") + "\n\n");
            return message.ToString().TrimEnd('\r', '\n');
        }

        return null;
    }

    private async Task StopPolling()
    {
        _searchCancel?.Cancel();

        if (_searchTask != null)
        {
            await Task.WhenAny(_searchTask);
        }

        StopPolling_UI_Actions();
    }

    private void StopPolling_UI_Actions()
    {
        _actionButtonContentKey = ButtonContent_Start_Key;
        this.RaisePropertyChanged(nameof(ActionButtonContent));
        SearchInProcess = false;

        ProgressBar_Value = ProgressBar_Minimum;
    }

    private async Task SearchDevices(CancellationToken taskCancel)
    {
        try
        {
            var searchFunction = AllSearchFunctions.FirstOrDefault(e => e.DisplayedName == SelectedFunction);

            if (searchFunction == null)
                throw new Exception(_localization.Get("Exception.SuitableFunctionNotFound", SelectedFunction ?? string.Empty));

            // Для демонстрации неодходимо оставить только эти вызовы метода, остальные закомментировать.
            //ViewSlaveAddress(42);
            //ViewSlaveAddress(98);
            //ViewSlaveAddress(182);

            for (int i = ProgressBar_Minimum; i <= ProgressBar_Maximum; i++)
            {
                try
                {
                    searchFunction.Message.SlaveID = (byte)i;

                    CurrentSlaveID = $"{i} (0x{i.ToString("X2")})";

                    await _modbusModel.ReadRegister(searchFunction.ReadFunction, searchFunction.Message, _messageType);

                    ViewSlaveAddress(i);
                }

                catch (TimeoutException)
                {
                    continue;
                }

                catch (ModbusException)
                {
                    ViewSlaveAddress(i);
                }

                finally
                {
                    ProgressBar_Value++;

                    await Task.Delay((int)_pauseBetweenRequests_ForWork, taskCancel);
                }
            }

            if (SlavesAddresses == string.Empty)
            {
                ErrorIsVisible = true;
            }
        }

        catch (OperationCanceledException)
        {

        }

        catch (Exception error)
        {
            _messageBox.Show(error.Message, MessageType.Error, error);
        }

        finally
        {
            StopPolling_UI_Actions();
        }
    }

    #region Валидация

    public string GetFieldViewName(string fieldName)
    {
        switch (fieldName)
        {
            case nameof(PauseBetweenRequests):
                return _localization.Get("Common.Pause");

            default:
                return fieldName;
        }
    }
    
    protected override ValidateMessage? GetErrorMessage(string fieldName, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        switch (fieldName)
        {
            case nameof(PauseBetweenRequests):
                return Check_Pause(value);
        }

        return null;
    }

    private ValidateMessage? Check_Pause(string value)
    {
        if (!StringValue.IsValidNumber(value, NumberStyles.Number, out _pauseBetweenRequests_ForWork))
        {
            return AllErrorMessages[DecError_uint];
        }

        return null;
    }

    #endregion Валидация
}
