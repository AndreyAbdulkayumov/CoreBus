using Core.Clients;
using Core.Clients.DataTypes;
using Core.Models;
using Core.Models.Modbus;
using Core.Models.Modbus.DataTypes;
using Core.Models.Modbus.Message;
using Core.Models.Settings.DataTypes;
using Core.Models.Settings.FileTypes;
using DynamicData;
using MessageBox.Core;
using MessageBusTypes.Macros;
using MessageBusTypes.ModbusClient;
using ReactiveUI;
using Services.Interfaces;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using ViewModels.ModbusClient.DataTypes;
using ViewModels.ModbusClient.ModbusRepresentations;

namespace ViewModels.ModbusClient;

public class ModbusClient_VM : ReactiveObject
{
    public const string ViewContent_NumberStyle_dec = "(dec)";
    public const string ViewContent_NumberStyle_hex = "(hex)";

    // Добавлять данные в DataGrid можно только из UI потока.
    // Поэтому используется событие, обработчик которого вызывается в behind code у файла с разметкой DataGrid.
    public static event EventHandler<ModbusDataDisplayed?>? AddDataOnTable;

    public event EventHandler<bool>? CheckSum_VisibilityChanged;

    private object? _modbusRequestBuilder_VM;

    public object? ModbusRequestBuilder_VM
    {
        get => _modbusRequestBuilder_VM;
        set => this.RaiseAndSetIfChanged(ref _modbusRequestBuilder_VM, value);
    }

    #region Properties

    private bool ui_IsEnable = false;

    public bool UI_IsEnable
    {
        get => ui_IsEnable;
        set => this.RaiseAndSetIfChanged(ref ui_IsEnable, value);
    }

    private bool _isMonitoringMode = false;

    public bool IsMonitoringMode
    {
        get => _isMonitoringMode;
        set => this.RaiseAndSetIfChanged(ref _isMonitoringMode, value);
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

    #endregion

    #region Representations Data

    private ObservableCollection<RequestResponseField_ItemData> _requestResponseItems = new ObservableCollection<RequestResponseField_ItemData>();

    public ObservableCollection<RequestResponseField_ItemData> RequestResponseItems
    {
        get => _requestResponseItems;
        set => this.RaiseAndSetIfChanged(ref _requestResponseItems, value);
    }

    private string _logData = string.Empty;

    public string LogData
    {
        get => _logData;
        set => this.RaiseAndSetIfChanged(ref _logData, value);
    }

    private ObservableCollection<BinaryRepresentation_ItemData> _binaryRepresentationItems = new ObservableCollection<BinaryRepresentation_ItemData>();

    public ObservableCollection<BinaryRepresentation_ItemData> BinaryRepresentationItems
    {
        get => _binaryRepresentationItems;
        set => this.RaiseAndSetIfChanged(ref _binaryRepresentationItems, value);
    }

    private ObservableCollection<FloatRepresentation_ItemData> _floatRepresentationItems = new ObservableCollection<FloatRepresentation_ItemData>();

    public ObservableCollection<FloatRepresentation_ItemData> FloatRepresentationItems
    {
        get => _floatRepresentationItems;
        set => this.RaiseAndSetIfChanged(ref _floatRepresentationItems, value);
    }

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> Command_Open_ModbusScanner { get; }
    public ReactiveCommand<Unit, Unit> Command_ClearData { get; }

    public ReactiveCommand<Unit, Unit> Command_Copy_Request { get; }
    public ReactiveCommand<Unit, Unit> Command_Copy_Response { get; }

    #endregion

    public static ModbusMessage? ModbusMessageType { get; private set; }


    private readonly IUIService _uiServices;
    private readonly IOpenChildWindowService _openChildWindow;
    private readonly IMessageBoxMainWindow _messageBox;
    private readonly ConnectedHost _connectedHostModel;
    private readonly Model_Modbus _modbusModel;
    private readonly ModbusClient_Mode_Normal_VM _normalMode_VM;

    private ushort _packageNumber = 0;

    private ModbusFunction? _currentFunction;


    public ModbusClient_VM(IUIService uiServices, IOpenChildWindowService openChildWindow, IMessageBoxMainWindow messageBox,
        ConnectedHost connectedHostModel, Model_Modbus modbusModel,
        ModbusClient_Mode_Normal_VM normalMode_VM)
    {
        _uiServices = uiServices ?? throw new ArgumentNullException(nameof(uiServices));
        _openChildWindow = openChildWindow ?? throw new ArgumentNullException(nameof(openChildWindow));
        _messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));
        _connectedHostModel = connectedHostModel ?? throw new ArgumentNullException(nameof(connectedHostModel));
        _modbusModel = modbusModel ?? throw new ArgumentNullException(nameof(modbusModel));
        _normalMode_VM = normalMode_VM ?? throw new ArgumentNullException(nameof(normalMode_VM));

        _connectedHostModel.DeviceIsConnect += Model_DeviceIsConnect;
        _connectedHostModel.DeviceIsDisconnected += Model_DeviceIsDisconnected;

        _normalMode_VM = normalMode_VM;
        _normalMode_VM.Subscribe(this);

        ModbusRequestBuilder_VM = _normalMode_VM;

        /****************************************************/
        //
        // Настройка прослушивания MessageBus
        //
        /****************************************************/

        MessageBus.Current.Listen<ModbusReadMessage>()
            .Subscribe(async message =>
            {
                await Receive_ReadMessage_Handler(message);
            });

        MessageBus.Current.Listen<ModbusWriteMessage>()
            .Subscribe(async message =>
            {
                await Receive_WriteMessage_Handler(message);
            });

        MessageBus.Current.Listen<MacrosContent<ModbusAdditionalData, MacrosCommandModbus>>()
            .Subscribe(async macros =>
            {
                await Receive_ListMessage_Handler(macros);
            });

        /****************************************************/
        //
        // Настройка свойств и команд модели отображения
        //
        /****************************************************/

        Command_Copy_Request = ReactiveCommand.CreateFromTask(async () =>
        {
            string Data = string.Empty;

            foreach (var element in RequestResponseItems)
            {
                if (element.RequestData != null)
                {
                    Data += element.RequestData + " ";
                }
            }

            await _uiServices.CopyToClipboard(Data);
        });
        Command_Copy_Request.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка копирования запроса в буфер обмена.\n\n{error.Message}", MessageType.Error, error));

        Command_Copy_Response = ReactiveCommand.CreateFromTask(async () =>
        {
            string Data = string.Empty;

            foreach (var element in RequestResponseItems)
            {
                if (element.ResponseData != null)
                {
                    Data += element.ResponseData + " ";
                }
            }

            await _uiServices.CopyToClipboard(Data);
        });
        Command_Copy_Response.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка копирования ответа в буфер обмена.\n\n{error.Message}", MessageType.Error, error));

        Command_Open_ModbusScanner = ReactiveCommand.CreateFromTask(_openChildWindow.ModbusScanner);

        Command_ClearData = ReactiveCommand.Create(() =>
        {
            AddDataOnTable?.Invoke(this, null);

            RequestResponseItems.Clear();
            LogData = string.Empty;
        });
        Command_ClearData.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка очистки данных.\n\n{error.Message}", MessageType.Error, error));

        this.WhenAnyValue(x => x.IsMonitoringMode)
            .Subscribe(_ =>
            {
                //if (!IsMonitoringMode)
                //{
                //    _cycleMode_VM.StopPolling();
                //}

                //CurrentModeViewModel = IsMonitoringMode ? _cycleMode_VM : _normalMode_VM;

                //CurrentModeViewModel = IsMonitoringMode ? _monitoringMode_VM : _normalMode_VM;                
            });

        this.WhenAnyValue(x => x.SelectedModbusType)
            .WhereNotNull()
            .Subscribe(x =>
            {
                if (_connectedHostModel.HostIsConnect)
                {
                    SetCheckSumVisiblity();

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
    }

    private void SendMacrosActionResponse(MacrosContent<ModbusAdditionalData, MacrosCommandModbus> macros, bool actionSuccess, string message, MessageType type, Exception? error = null)
    {
        MessageBus.Current.SendMessage(new MacrosActionResponse(macros.MacrosName, macros.Sender, actionSuccess, message, type, error));
    }

    private void SendCommandActionResponse(string? sender, bool actionSuccess, string message, MessageType type, Exception? error = null)
    {
        MessageBus.Current.SendMessage(new MacrosActionResponse(null, sender, actionSuccess, message, type, error));
    }

    private async Task Receive_ReadMessage_Handler(ModbusReadMessage message)
    {
        try
        {
            await Modbus_Read(message.SlaveID, message.Address, message.Function, message.NumberOfRegisters, message.CheckSum_IsEnable);
        }

        catch (Exception error)
        {
            //if (_cycleMode_VM.IsStart)
            //    _cycleMode_VM.StopPolling();

            if (message.Sender == MainWindow_VM.SenderName)
            {
                _messageBox.Show(error.Message, MessageType.Error, error);
                return;
            }

            SendCommandActionResponse(message.Sender, false, error.Message, MessageType.Error, error);
        }
    }

    private async Task Receive_WriteMessage_Handler(ModbusWriteMessage message)
    {
        try
        {
            await Modbus_Write(message.SlaveID, message.Address, message.Function, message.WriteData, message.NumberOfRegisters, message.CheckSum_IsEnable);
        }

        catch (Exception error)
        {
            if (message.Sender == MainWindow_VM.SenderName)
            {
                _messageBox.Show(error.Message, MessageType.Error, error);
                return;
            }

            SendCommandActionResponse(message.Sender, false, error.Message, MessageType.Error, error);
        }
    }

    private async Task Receive_ListMessage_Handler(MacrosContent<ModbusAdditionalData, MacrosCommandModbus> macros)
    {
        if (!_connectedHostModel.HostIsConnect)
        {
            SendMacrosActionResponse(macros, false, "Клиент отключен.", MessageType.Error);
            return;
        }

        if (macros.Commands == null || macros.Commands.Count == 0)
        {
            SendMacrosActionResponse(macros, false, $"Макрос {macros.MacrosName} не содержит команд.", MessageType.Warning);
            return;
        }

        var errorMessages = new List<string>();

        MacrosCommandModbus? currentCommand = null;

        string messageSeparator = "\n\n---------------------------\n\n";

        foreach (var command in macros.Commands)
        {
            try
            {
                if (command.Content == null)
                    continue;

                currentCommand = command;

                var modbusFunction = Function.AllFunctions.Single(x => x.Number == command.Content.FunctionNumber);

                if (modbusFunction == null)
                    continue;

                if (modbusFunction is ModbusReadFunction readFunction)
                {
                    await ReadAction(command.Content.SlaveID, command.Content.Address, readFunction, command.Content.NumberOfReadRegisters, command.Content.CheckSum_IsEnable);
                }

                else if (modbusFunction is ModbusWriteFunction writeFunction)
                {
                    await WriteAction(
                        command.Content.SlaveID,
                        command.Content.Address,
                        writeFunction,
                        command.Content.WriteInfo?.WriteBuffer,
                        command.Content.WriteInfo != null ? command.Content.WriteInfo.NumberOfWriteRegisters : 0,
                        command.Content.CheckSum_IsEnable
                        );
                }

                else
                {
                    throw new Exception("Выбранна неизвестная Modbus функция");
                }
            }

            catch (ModbusException error)
            {
                if (currentCommand == null || currentCommand.Content == null)
                    continue;

                var exceptionMessage = await ModbusErrorHandler(currentCommand.Content.Address, error);

                errorMessages.Add($"Ошибка в команде \"{currentCommand.Name}\".\n\n{exceptionMessage}");
            }

            catch (Exception error)
            {
                errorMessages.Add($"Ошибка в команде \"{currentCommand?.Name}\".\n\n{error.Message}");
            }
        }

        if (errorMessages.Any())
        {
            errorMessages.Insert(0, $"При выполнении макроса \"{macros.MacrosName}\" произошли ошибки.");

            SendMacrosActionResponse(macros, false, string.Join(messageSeparator, errorMessages), MessageType.Error);
        }
    }

    private void SetCheckSumVisiblity()
    {
        bool isVisible = !_connectedHostModel.HostIsConnect || SelectedModbusType != Modbus_TCP_Name;

        CheckSum_VisibilityChanged?.Invoke(this, isVisible);
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

        SetCheckSumVisiblity();

        UI_IsEnable = true;
    }

    private void Model_DeviceIsDisconnected(object? sender, IConnection? e)
    {
        UI_IsEnable = false;

        ButtonModbusScanner_IsVisible = true;

        SetCheckSumVisiblity();

        _packageNumber = 0;
    }

    private async Task Modbus_Read(byte slaveID, ushort address, ModbusReadFunction readFunction, int numberOfRegisters, bool checkSum_Enable)
    {
        try
        {
            await ReadAction(slaveID, address, readFunction, numberOfRegisters, checkSum_Enable);
        }

        catch (ModbusException error)
        {
            var message = await ModbusErrorHandler(address, error);

            throw new Exception(message);
        }

        catch (Exception error)
        {
            var info = error.InnerException as ModbusExceptionInfo;

            if (info != null)
            {
                await AddDataOnView(null, info.Details);
            }

            throw new Exception(error.Message);
        }
    }

    private async Task Modbus_Write(byte slaveID, ushort address, ModbusWriteFunction writeFunction, byte[]? modbusWriteData, int numberOfRegisters, bool checkSum_Enable)
    {
        try
        {
            await WriteAction(slaveID, address, writeFunction, modbusWriteData, numberOfRegisters, checkSum_Enable);
        }

        catch (ModbusException error)
        {
            var message = await ModbusErrorHandler(address, error);

            throw new Exception(message);
        }

        catch (Exception error)
        {
            var Info = error.InnerException as ModbusExceptionInfo;

            if (Info != null)
            {
                await AddDataOnView(null, Info.Details);
            }

            throw new Exception(error.Message);
        }
    }

    private async Task ReadAction(byte slaveID, ushort address, ModbusReadFunction readFunction, int numberOfRegisters, bool checkSum_Enable)
    {
        if (_connectedHostModel.HostIsConnect == false)
        {
            throw new Exception("Клиент отключен.");
        }

        if (ModbusMessageType == null)
        {
            throw new Exception("Не задан тип протокола Modbus.");
        }

        if (numberOfRegisters < 1)
        {
            throw new Exception("Сколько, сколько регистров вы хотите прочитать? :)");
        }

        _currentFunction = readFunction;

        MessageData data = new ReadTypeMessage(
            slaveID,
            address,
            numberOfRegisters,
            ModbusMessageType is ModbusTCP_Message ? false : checkSum_Enable);

        ModbusOperationResult? result = await _modbusModel.ReadRegister(
                        readFunction,
                        data,
                        ModbusMessageType);

        await AddDataOnView(new ModbusDataDisplayed()
        {
            OperationID = _packageNumber,
            FuncNumber = readFunction.DisplayedNumber,
            Address = address,
            ViewAddress = CreateViewAddress(address, result.ReadedData == null ? 0 : numberOfRegisters),
            Data = result.ReadedData,
            ViewData = CreateViewData(result.ReadedData, readFunction, numberOfRegisters)
        },
        result.Details);
    }

    private async Task WriteAction(byte slaveID, ushort address, ModbusWriteFunction writeFunction, byte[]? modbusWriteData, int numberOfRegisters, bool checkSum_Enable)
    {
        if (_connectedHostModel.HostIsConnect == false)
        {
            throw new Exception("Клиент отключен.");
        }

        if (ModbusMessageType == null)
        {
            throw new Exception("Не задан тип протокола Modbus.");
        }

        if (modbusWriteData == null || modbusWriteData.Length == 0)
        {
            throw new Exception("Укажите данные для записи.");
        }

        _currentFunction = writeFunction;

        MessageData data = new WriteTypeMessage(
            slaveID,
            address,
            modbusWriteData,
            numberOfRegisters,
            ModbusMessageType is ModbusTCP_Message ? false : checkSum_Enable);

        ModbusOperationResult result = await _modbusModel.WriteRegister(
            writeFunction,
            data,
            ModbusMessageType);

        await AddDataOnView(new ModbusDataDisplayed()
        {
            OperationID = _packageNumber,
            FuncNumber = writeFunction.DisplayedNumber,
            Address = address,
            ViewAddress = CreateViewAddress(address, numberOfRegisters),
            Data = modbusWriteData,
            ViewData = CreateViewData(modbusWriteData, writeFunction, numberOfRegisters)
        },
        result.Details);
    }

    private async Task<string> ModbusErrorHandler(ushort address, ModbusException error)
    {
        await AddDataOnView(new ModbusDataDisplayed()
        {
            OperationID = _packageNumber,
            FuncNumber = _currentFunction?.DisplayedNumber,
            Address = address,
            ViewAddress = CreateViewAddress(address, 1),
            Data = Array.Empty<byte>(),
            ViewData = $"Ошибка Modbus.\nКод: {error.ErrorCode.ToString()}"
        },
        error.Details);

        string addition = string.Empty;

        if (_currentFunction == Function.ForceSingleCoil && error.ErrorCode == 3)
        {
            addition = $"\n\nВ функции {Function.ForceSingleCoil.DisplayedName} используется логический тип данных.\n" +
                "\nTrue - это 0xFF00" +
                "\nFalse - это 0x0000";
        }

        return
            "Ошибка Modbus.\n\n" +
            $"Код функции: {error.FunctionCode.ToString()}\n" +
            $"Код ошибки: {error.ErrorCode.ToString()}\n\n" +
            error.Message +
            addition;
    }

    private (string[], string) ParseData(byte[]? data)
    {
        if (data == null)
        {
            return (Array.Empty<string>(), string.Empty);
        }

        var dataBytes = new string[data.Length];
        string stringForLog = string.Empty;

        for (int i = 0; i < data.Length; i++)
        {
            dataBytes[i] = data[i].ToString("X2");
            stringForLog += data[i].ToString("X2") + "   ";
        }

        return (dataBytes, stringForLog);
    }

    /*************************************************************************/
    //
    // Следующие три метода используются в Modbus_Mode_Cycle_VM
    //
    /*************************************************************************/

    public async Task AddDataOnView(ModbusDataDisplayed? data, ModbusActionDetails? details)
    {
        if (data != null)
        {
            AddDataOnTable?.Invoke(this, data);

            await _uiServices.RunInUIThread(() =>
            {
                BinaryRepresentationItems.Clear();

                var binaryItems = BinaryRepresentation.GetData(data, _messageBox, _uiServices.CopyToClipboard);

                if (binaryItems != null)
                {
                    BinaryRepresentationItems.AddRange(binaryItems);
                }
            });

            await _uiServices.RunInUIThread(() =>
            {
                FloatRepresentationItems.Clear();

                var floatItems = FloatRepresentation.GetData(data);

                if (floatItems != null)
                {
                    FloatRepresentationItems.AddRange(floatItems);
                }
            });
        }

        if (details != null)
        {
            (string[] Bytes, string LogString) request = ParseData(details.RequestBytes);
            (string[] Bytes, string LogString) response = ParseData(details.ResponseBytes);

            await _uiServices.RunInUIThread(() =>
            {
                RequestResponseItems.Clear();
                RequestResponseItems.AddRange(LastRequestRepresentation.GetData(request.Bytes, response.Bytes));
            });

            if (LogData != string.Empty)
            {
                LogData += "\n\n";
            }

            LogData += LogRepresentation.GetData(
                details.Request_ExecutionTime, request.LogString,
                details.Response_ExecutionTime, response.LogString);
        }

        _packageNumber++;
    }

    public static string CreateViewAddress(ushort startAddress, int numberOfRegisters)
    {
        string displayedString = string.Empty;

        ushort currentAddress = startAddress;

        for (int i = 0; i < numberOfRegisters; i++)
        {
            displayedString += $"0x{currentAddress.ToString("X")} ({currentAddress.ToString()})";

            if (i != numberOfRegisters - 1)
            {
                displayedString += "\n";
            }

            currentAddress++;
        }

        return displayedString;
    }

    public static string CreateViewData(byte[]? modbusData, ModbusFunction function, int numberOfRegisters)
    {
        if (modbusData == null)
        {
            return string.Empty;
        }

        if (function.Number == Function.ForceMultipleCoils.Number ||
            function.Number == Function.ReadCoilStatus.Number ||
            function.Number == Function.ReadDiscreteInputs.Number)
        {
            return GetViewData_FromBytes(modbusData, numberOfRegisters);
        }

        return GetViewData_FromWords(modbusData);
    }

    private static string GetViewData_FromWords(byte[] modbusData)
    {
        string displayedString = string.Empty;

        UInt16 temp;

        for (int i = 0; i < modbusData.Length - 1; i += 2)
        {
            temp = (UInt16)((modbusData[i + 1] << 8) | modbusData[i]);

            displayedString += $"0x{temp.ToString("X")} ({temp.ToString()})\n";
        }

        // Обработка последнего байта, если длина массива нечетная
        if (modbusData.Length % 2 != 0)
        {
            temp = modbusData.Last();

            displayedString += $"0x{temp.ToString("X")} ({temp.ToString()})\n";
        }

        displayedString = displayedString.TrimEnd('\n');

        return displayedString;
    }

    private static string GetViewData_FromBytes(byte[] modbusData, int numberOfRegisters)
    {
        string displayedString = string.Empty;

        int registerCounter = 0;

        foreach (byte element in modbusData)
        {
            for (int i = 0; i < 8; i++)
            {
                if (registerCounter == numberOfRegisters)
                {
                    break;
                }

                displayedString += (element & (1 << (i))) != 0 ? "1\n" : "0\n";
                registerCounter++;
            }
        }

        displayedString = displayedString.TrimEnd('\n');

        return displayedString;
    }
}
