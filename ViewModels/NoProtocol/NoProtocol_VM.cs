using Core.Clients;
using Core.Clients.DataTypes;
using Core.Models;
using Core.Models.NoProtocol;
using Core.Models.NoProtocol.DataTypes;
using Core.Models.Settings.DataTypes;
using Core.Models.Settings.FileTypes;
using MessageBox.Core;
using MessageBusTypes.Macros;
using MessageBusTypes.NoProtocol;
using ReactiveUI;
using Services.Interfaces;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Text;
using ViewModels.Helpers;

namespace ViewModels.NoProtocol;

public class NoProtocol_VM : ReactiveObject
{
    private object? _currentModeViewModel;

    public object? CurrentModeViewModel
    {
        get => _currentModeViewModel;
        set => this.RaiseAndSetIfChanged(ref _currentModeViewModel, value);
    }

    #region Properties

    private bool ui_IsEnable = false;

    public bool UI_IsEnable
    {
        get => ui_IsEnable;
        set => this.RaiseAndSetIfChanged(ref ui_IsEnable, value);
    }

    // Ключи режимов отправки (локализуемые ярлыки выводятся через ILocalizationService).
    public const string SendMode_Normal = "NoProtocol.SendMode.Normal";
    public const string SendMode_Cycle = "NoProtocol.SendMode.Cycle";
    public const string SendMode_Files = "NoProtocol.SendMode.Files";

    private string _selectedSendModeKey = SendMode_Normal;

    /// <summary>
    /// Ключ текущего выбранного режима отправки (для биндинга в ComboBox).
    /// </summary>
    public string SelectedSendMode
    {
        get => _selectedSendModeKey;
        set => this.RaiseAndSetIfChanged(ref _selectedSendModeKey, value);
    }

    private ObservableCollection<string> _allSendModes = new ObservableCollection<string>()
    {
        SendMode_Normal, SendMode_Cycle, SendMode_Files
    };

    public ObservableCollection<string> AllSendModes
    {
        get => _allSendModes;
    }

    private const string InterfaceType_Default = "Common.Undefined";
    private const string InterfaceType_SerialPort = "Serial Port";
    private const string InterfaceType_Ethernet = "Ethernet";

    private string _interfaceTypeKey = InterfaceType_Default;

    /// <summary>
    /// Локализованный тип интерфейса: либо "Serial Port"/"Ethernet" (как есть),
    /// либо значение по ключу локализации (для "не определен" / "undefined").
    /// </summary>
    public string InterfaceType =>
        _interfaceTypeKey == InterfaceType_SerialPort || _interfaceTypeKey == InterfaceType_Ethernet
            ? _interfaceTypeKey
            : _localization[_interfaceTypeKey];

    private string? _selectedEncoding;

    public string? SelectedEncoding
    {
        get => _selectedEncoding;
        set => this.RaiseAndSetIfChanged(ref _selectedEncoding, value);
    }

    private bool _encodingIsVisible = true;

    public bool EncodingIsVisible
    {
        get => _encodingIsVisible;
        set => this.RaiseAndSetIfChanged(ref _encodingIsVisible, value);
    }

    private string _rx_String = string.Empty;

    public string RX_String
    {
        get => _rx_String;
        set => this.RaiseAndSetIfChanged(ref _rx_String, value);
    }

    private bool _rx_NextLine;

    public bool RX_NextLine
    {
        get => _rx_NextLine;
        set => this.RaiseAndSetIfChanged(ref _rx_NextLine, value);
    }

    private bool _rx_IsByteView = false;

    public bool RX_IsByteView
    {
        get => _rx_IsByteView;
        set => this.RaiseAndSetIfChanged(ref _rx_IsByteView, value);
    }

    #endregion

    public ReactiveCommand<Unit, Unit> Command_ClearRX { get; }

    private const int MaxCapacity = 3000;

    // Делаем эти значения емкости одинаковыми, чтобы не тратить ресурсы на дополнительное выделение памяти.
    private readonly StringBuilder RX = new StringBuilder(MaxCapacity, MaxCapacity);

    private const string BytesSeparator = " ";
    private const string ElementSeparatorInCycleMode = "  ";

    private readonly IMessageBoxMainWindow _messageBox;
    private readonly ConnectedHost _connectedHostModel;
    private readonly Model_NoProtocol _noProtocolModel;
    private readonly NoProtocol_Mode_Normal_VM _normalMode_VM;
    private readonly NoProtocol_Mode_Cycle_VM _cycleMode_VM;
    private readonly NoProtocol_Mode_Files_VM _filesMode_VM;
    private readonly ILocalizationService _localization;

    public NoProtocol_VM(IMessageBoxMainWindow messageBox,
        ConnectedHost connectedHostModel, Model_NoProtocol noProtocolModel,
        NoProtocol_Mode_Normal_VM normalMode_VM, NoProtocol_Mode_Cycle_VM cycleMode_VM, NoProtocol_Mode_Files_VM filesMode_VM,
        ILocalizationService localization)
    {
        _messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));
        _connectedHostModel = connectedHostModel ?? throw new ArgumentNullException(nameof(connectedHostModel));
        _noProtocolModel = noProtocolModel ?? throw new ArgumentNullException(nameof(noProtocolModel));
        _normalMode_VM = normalMode_VM ?? throw new ArgumentNullException(nameof(normalMode_VM));
        _cycleMode_VM = cycleMode_VM ?? throw new ArgumentNullException(nameof(cycleMode_VM));
        _filesMode_VM = filesMode_VM ?? throw new ArgumentNullException(nameof(filesMode_VM));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));

        _localization.LanguageChanged += (_, _) =>
        {
            this.RaisePropertyChanged(nameof(InterfaceType));
        };

        _connectedHostModel.DeviceIsConnect += Model_DeviceIsConnect;
        _connectedHostModel.DeviceIsDisconnected += Model_DeviceIsDisconnected;

        _noProtocolModel.Model_DataReceived += NoProtocol_Model_DataReceived;
        _noProtocolModel.Model_ErrorInReadThread += NoProtocol_Model_ErrorInReadThread;

        MessageBus.Current.Listen<NoProtocolSendMessage>()
            .Subscribe(async message =>
            {
                await Receive_SendMessage_Handler(message);
            });

        MessageBus.Current.Listen<MacrosContent<object, MacrosCommandNoProtocol>>()
            .Subscribe(async macros =>
            {
                await Receive_ListMessage_Handler(macros);
            });

        Command_ClearRX = ReactiveCommand.Create(() => { RX?.Clear(); RX_String = string.Empty; });

        this.WhenAnyValue(x => x.SelectedSendMode)
            .Subscribe(_ =>
            {
                if (SelectedSendMode != SendMode_Cycle)
                {
                    _cycleMode_VM.StopPolling();
                }

                EncodingIsVisible = SelectedSendMode != SendMode_Files;

                switch (SelectedSendMode)
                {
                    case SendMode_Normal:
                        CurrentModeViewModel = _normalMode_VM;
                        break;

                    case SendMode_Cycle:
                        CurrentModeViewModel = _cycleMode_VM;
                        break;

                    case SendMode_Files:
                        CurrentModeViewModel = _filesMode_VM;
                        break;
                }
            });
    }

    private void SendMacrosActionResponse(MacrosContent<object, MacrosCommandNoProtocol> macros, bool actionSuccess, string message, MessageType type, Exception? error = null)
    {
        MessageBus.Current.SendMessage(new MacrosActionResponse(macros.MacrosName, macros.Sender, actionSuccess, message, type, error));
    }

    private void SendCommandActionResponse(string? sender, bool actionSuccess, string message, MessageType type, Exception? error = null)
    {
        MessageBus.Current.SendMessage(new MacrosActionResponse(null, sender, actionSuccess, message, type, error));
    }

    private async Task Receive_SendMessage_Handler(NoProtocolSendMessage message)
    {
        try
        {
            byte[] buffer = CreateSendBuffer(message.IsBytes, message.Message, message.EnableCR, message.EnableLF, message.SelectedEncoding);

            await _noProtocolModel.SendBytes(buffer);
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

    private async Task Receive_ListMessage_Handler(MacrosContent<object, MacrosCommandNoProtocol> macros)
    {
        if (!_connectedHostModel.HostIsConnect)
        {
            SendMacrosActionResponse(macros, false, _localization.Get("Macros.ClientDisconnected"), MessageType.Error);
            return;
        }

        if (macros.Commands == null || macros.Commands.Count == 0)
        {
            SendMacrosActionResponse(macros, false, _localization.Get("Macros.NoCommands", macros.MacrosName ?? string.Empty), MessageType.Warning);
            return;
        }

        var errorMessages = new List<string>();

        MacrosCommandNoProtocol? currentCommand = null;

        string messageSeparator = "\n\n---------------------------\n\n";

        foreach (var command in macros.Commands)
        {
            try
            {
                if (command.Content == null)
                    continue;

                currentCommand = command;

                byte[] buffer = CreateSendBuffer(
                    command.Content.IsByteString,
                    command.Content.Message,
                    command.Content.EnableCR,
                    command.Content.EnableLF,
                    AppEncoding.GetEncoding(command.Content.MacrosEncoding)
                    );

                await _noProtocolModel.SendBytes(buffer);
            }

            catch (Exception error)
            {
                errorMessages.Add(_localization.Get("Macros.CommandErrorPrefix", currentCommand?.Name ?? string.Empty) + "\n\n" + error.Message);
            }
        }

        if (errorMessages.Any())
        {
            errorMessages.Insert(0, _localization.Get("Macros.MacroErrorsHeader", macros.MacrosName ?? string.Empty));

            SendMacrosActionResponse(macros, false, string.Join(messageSeparator, errorMessages), MessageType.Error);
        }
    }

    public static byte[] CreateSendBuffer(bool isBytes, string? message, bool enableCR, bool enableLF, Encoding encoding)
    {
        if (string.IsNullOrEmpty(message))
        {
            throw new Exception(LocalizationProvider.Get("Common.NoDataToSend"));
        }

        var buffer = new List<byte>(
            isBytes ?
                StringByteConverter.ByteStringToByteArray(message) :
                encoding.GetBytes(message)
                );

        if (enableCR == true)
        {
            buffer.Add((byte)'\r');
        }

        if (enableLF == true)
        {
            buffer.Add((byte)'\n');
        }

        return buffer.ToArray();
    }

    private void Model_DeviceIsConnect(object? sender, IConnection? e)
    {
        if (e is IPClient)
        {
            _interfaceTypeKey = InterfaceType_Ethernet;
        }

        else if (e is SerialPortClient)
        {
            _interfaceTypeKey = InterfaceType_SerialPort;
        }

        else
        {
            _messageBox.Show(_localization.Get("Exception.UnknownConnectionTypeSimple"), MessageType.Warning);
            return;
        }

        this.RaisePropertyChanged(nameof(InterfaceType));
        UI_IsEnable = true;
    }

    private void Model_DeviceIsDisconnected(object? sender, IConnection? e)
    {
        _interfaceTypeKey = InterfaceType_Default;
        this.RaisePropertyChanged(nameof(InterfaceType));

        UI_IsEnable = false;
    }

    private void NoProtocol_Model_DataReceived(object? sender, NoProtocolDataReceivedEventArgs e)
    {
        string stringData = RX_IsByteView ?
            BitConverter.ToString(e.RawData).Replace("-", BytesSeparator) + BytesSeparator :
            ConnectedHost.GlobalEncoding.GetString(e.RawData);

        if (e.DataWithDebugInfo != null)
        {
            e.DataWithDebugInfo[e.DataIndex] = stringData;
            stringData = string.Join(ElementSeparatorInCycleMode, e.DataWithDebugInfo);
        }

        if (RX_NextLine)
        {
            stringData += Environment.NewLine;
        }

        // Если входные данные слишком длинные, то оставляем только хвост
        if (stringData.Length > RX.MaxCapacity)
        {            
            stringData = stringData.Substring(stringData.Length - RX.MaxCapacity);
            RX.Clear();
        }

        else
        {
            int totalLength = RX.Length + stringData.Length;

            if (totalLength > RX.MaxCapacity)
            {
                int removeCount = totalLength - RX.MaxCapacity;
                RX.Remove(0, Math.Min(removeCount, RX.Length));
            }
        }

        RX.Append(stringData);

        RX_String = RX.ToString();
    }

    private void NoProtocol_Model_ErrorInReadThread(object? sender, Exception e)
    {
        _messageBox.Show(_localization.Get("Error.AsyncReadError") + "\n\n" + e.Message, MessageType.Error, e);
    }
}
