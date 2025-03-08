﻿using ReactiveUI;
using MessageBox_Core;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using ViewModels.NoProtocol;
using ViewModels.ModbusClient;
using ViewModels.Helpers;
using ViewModels.Settings.MessageBusTypes;
using Core.Models;
using Core.Models.Settings;
using Core.Models.AppUpdateSystem;
using Core.Models.Settings.FileTypes;
using Core.Models.AppUpdateSystem.DataTypes;
using Core.Clients.DataTypes;
using Services.Interfaces;

namespace ViewModels
{
    public enum ApplicationWorkMode
    {
        NoProtocol,
        ModbusClient
    }

    public class MainWindow_VM : ReactiveObject
    {
        public static string? SettingsDocument;

        private static ApplicationWorkMode _currentApplicationWorkMode;

        public static ApplicationWorkMode CurrentApplicationWorkMode
        {
            get => _currentApplicationWorkMode;
            private set
            {
                _currentApplicationWorkMode = value;
                ApplicationWorkModeChanged?.Invoke(null, value);
            }
        }

        public static event EventHandler<ApplicationWorkMode>? ApplicationWorkModeChanged;

        private bool _ui_IsConnectedState = false;

        public bool UI_IsConnectedState
        {
            get => _ui_IsConnectedState;
            set => this.RaiseAndSetIfChanged(ref _ui_IsConnectedState, value);
        }

        private object? _currentViewModel;

        public object? CurrentViewModel
        {
            get => _currentViewModel;
            set => this.RaiseAndSetIfChanged(ref _currentViewModel, value);
        }

        private ObservableCollection<string> _presets = new ObservableCollection<string>();

        public ObservableCollection<string> Presets
        {
            get => _presets;
            set => this.RaiseAndSetIfChanged(ref _presets, value);
        }

        private string? _selectedPreset;

        public string? SelectedPreset
        {
            get => _selectedPreset;
            set => this.RaiseAndSetIfChanged(ref _selectedPreset, value);
        }

        public ReactiveCommand<Unit, Unit> Command_OpenSettingsWindow { get; }
        public ReactiveCommand<Unit, Unit> Command_OpenAboutWindow { get; }
        public ReactiveCommand<Unit, Unit> Command_OpenUserManual { get; }
        
        public ReactiveCommand<Unit, Unit> Command_ProtocolMode_NoProtocol { get; }
        public ReactiveCommand<Unit, Unit> Command_ProtocolMode_Modbus { get; }
        public ReactiveCommand<Unit, Unit> Command_OpenMacrosWindow { get; }

        public ReactiveCommand<Unit, Unit> Command_Connect { get; }
        public ReactiveCommand<Unit, Unit> Command_Disconnect { get; }

        private bool _updateMessageIsVisible = false;

        public bool UpdateMessageIsVisible
        {
            get => _updateMessageIsVisible;
            set => this.RaiseAndSetIfChanged(ref _updateMessageIsVisible, value);
        }

        private string _newAppVersion = string.Empty;

        public string NewAppVersion
        {
            get => _newAppVersion;
            set => this.RaiseAndSetIfChanged(ref _newAppVersion, value);
        }

        public ReactiveCommand<Unit, Unit> Command_UpdateApp { get; }
        public ReactiveCommand<Unit, Unit> Command_SkipNewAppVersion { get; }

        private string? _connectionString;

        public string? ConnectionString
        {
            get => _connectionString;
            set => this.RaiseAndSetIfChanged(ref _connectionString, value);
        }

        private const string ConnectionStatus_Connected = "Подключено";
        private const string ConnectionStatus_Disconnected = "Отключено";

        private string _connectionStatus = ConnectionStatus_Disconnected;

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => this.RaiseAndSetIfChanged(ref _connectionStatus, value);
        }

        private bool _connectionTimer_IsVisible = false;

        public bool ConnectionTimer_IsVisible
        {
            get => _connectionTimer_IsVisible;
            set => this.RaiseAndSetIfChanged(ref _connectionTimer_IsVisible, value);
        }

        private string _connectionTimer_View = string.Empty;

        public string ConnectionTimer_View
        {
            get => _connectionTimer_View;
            set => this.RaiseAndSetIfChanged(ref _connectionTimer_View, value);
        }

        private const int ConnectionTimer_Interval_ms = 1000;
        private readonly System.Timers.Timer _connectionTimer;

        private TimeSpan _connectionTime = new TimeSpan();

        // Нужно выставить true, а затем при инициализации false,
        // чтобы сработало выставление цвета после запуска программы
        // согласно заданной теме.
        private bool _led_TX_IsActive = true;

        public bool Led_TX_IsActive
        {
            get => _led_TX_IsActive;
            set => this.RaiseAndSetIfChanged(ref _led_TX_IsActive, value);
        }

        // Нужно выставить true, а затем при инициализации false,
        // чтобы сработало выставление цвета после запуска программы
        // согласно заданной теме.
        private bool _led_RX_IsActive = true;

        public bool Led_RX_IsActive
        {
            get => _led_RX_IsActive;
            set => this.RaiseAndSetIfChanged(ref _led_RX_IsActive, value);
        }

        private readonly IUIService _uiServices;
        private readonly IOpenChildWindowService _openChildWindowService;
        private readonly IFileSystemService _fileSystemService;
        private readonly IMessageBoxMainWindow _messageBox;

        private readonly NoProtocol_VM _noProtocol_VM;
        private readonly ModbusClient_VM _modbusClient_VM;

        private readonly ConnectedHost Model;
        private readonly Model_Settings SettingsFile;
        private readonly Model_AppUpdateSystem AppUpdateSystem;

        private readonly object TX_View_Locker = new object();
        private readonly object RX_View_Locker = new object();

        private string? _newAppDownloadLink;

        public MainWindow_VM(IUIService uiServices, IOpenChildWindowService openChildWindowService, IFileSystemService fileSystemService, IMessageBoxMainWindow messageBox, 
            NoProtocol_VM noProtocol_VM, ModbusClient_VM modbusClient_VM)
        {
            _uiServices = uiServices ?? throw new ArgumentNullException(nameof(uiServices));
            _openChildWindowService = openChildWindowService ?? throw new ArgumentNullException(nameof(openChildWindowService));
            _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
            _messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));

            _noProtocol_VM = noProtocol_VM ?? throw new ArgumentNullException(nameof(noProtocol_VM));
            _modbusClient_VM = modbusClient_VM ?? throw new ArgumentNullException(nameof(modbusClient_VM));

            Model = ConnectedHost.Model;
            SettingsFile = Model_Settings.Model;
            AppUpdateSystem = Model_AppUpdateSystem.Model;

            SettingsDocument = SettingsFile.AppData.SelectedPresetFileName;

            Model.DeviceIsConnect += Model_DeviceIsConnect;
            Model.DeviceIsDisconnected += Model_DeviceIsDisconnected;

            _connectionTimer = new System.Timers.Timer(ConnectionTimer_Interval_ms);
            _connectionTimer.Elapsed += ConnectionTimer_Elapsed;

            MessageBus.Current.Listen<PresetUpdateTriggerMessage>()
                .Subscribe(_ =>
                {
                    UpdateListOfPresets();
                });

            this.WhenAnyValue(x => x.SelectedPreset)
                .WhereNotNull()
                .Where(x => !string.IsNullOrEmpty(x))
                .Subscribe(PresetName =>
                {
                    try
                    {
                        SettingsFile.ReadPreset(PresetName);

                        if (SettingsFile.Settings != null)
                        {
                            string? encodingName = SettingsFile.Settings.GlobalEncoding;
                            Model.SetGlobalEncoding(AppEncoding.GetEncoding(encodingName));
                            _noProtocol_VM.SelectedEncoding = encodingName;
                        }

                        ConnectionString = GetConnectionString();

                        SettingsDocument = PresetName;
                        SettingsFile.AppData.SelectedPresetFileName = PresetName;
                    }

                    catch (Exception error)
                    {
                        _messageBox.Show("Ошибка выбора пресета.\n\n" + error.Message, MessageType.Error);
                    }
                });

            Command_OpenSettingsWindow = ReactiveCommand.CreateFromTask(async () => await _openChildWindowService.Settings());
            Command_OpenSettingsWindow.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка работы окна \"Настройки\".\n\n{error.Message}", MessageType.Error));

            Command_OpenAboutWindow = ReactiveCommand.CreateFromTask(async () => await _openChildWindowService.About());
            Command_OpenAboutWindow.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка работы окна \"О программе\".\n\n{error.Message}", MessageType.Error));

            Command_OpenUserManual = ReactiveCommand.Create(_fileSystemService.OpenUserManual);
            Command_OpenUserManual.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка открытия руководства пользователя.\n\n{error.Message}", MessageType.Error));

            Command_ProtocolMode_NoProtocol = ReactiveCommand.Create(() =>
            {
                CurrentViewModel = _noProtocol_VM;
                Model.SetProtocol_NoProtocol();

                SettingsFile.AppData.SelectedMode = AppMode.NoProtocol;
                CurrentApplicationWorkMode = ApplicationWorkMode.NoProtocol;
            });
            Command_ProtocolMode_NoProtocol.ThrownExceptions.Subscribe(error => _messageBox.Show(error.Message, MessageType.Error));

            Command_ProtocolMode_Modbus = ReactiveCommand.Create(() =>
            {
                CurrentViewModel = _modbusClient_VM;
                Model.SetProtocol_Modbus();

                SettingsFile.AppData.SelectedMode = AppMode.ModbusClient;
                CurrentApplicationWorkMode = ApplicationWorkMode.ModbusClient;
            });
            Command_ProtocolMode_Modbus.ThrownExceptions.Subscribe(error => _messageBox.Show(error.Message, MessageType.Error));

            Command_OpenMacrosWindow = ReactiveCommand.Create(_openChildWindowService.Macros);
            Command_OpenMacrosWindow.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка окна макросов.\n\n{error.Message}", MessageType.Error));

            Command_Connect = ReactiveCommand.Create(Connect_Handler);
            Command_Connect.ThrownExceptions.Subscribe(error => _messageBox.Show(error.Message, MessageType.Error));

            Command_Disconnect = ReactiveCommand.CreateFromTask(Model.Disconnect);
            Command_Disconnect.ThrownExceptions.Subscribe(error => _messageBox.Show(error.Message, MessageType.Error));

            Command_UpdateApp = ReactiveCommand.Create(() => AppUpdateSystem.GoToWebPage(_newAppDownloadLink));
            Command_UpdateApp.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка перехода по ссылке скачивания приложения:\n\n{error.Message}", MessageType.Error));

            Command_SkipNewAppVersion = ReactiveCommand.Create(() =>
            {
                SettingsFile.AppData.SkippedAppVersion = NewAppVersion;
                UpdateMessageIsVisible = false;
            });
            Command_SkipNewAppVersion.ThrownExceptions.Subscribe();

            // Действия после запуска приложения

            SetAppTheme(SettingsFile.AppData.ThemeName);

            SetAppMode(SettingsFile.AppData.SelectedMode);
        }

        private void SetAppTheme(AppTheme themeName)
        {
            switch (themeName)
            {
                case AppTheme.Dark:
                    _uiServices.Set_Dark_Theme();
                    break;

                case AppTheme.Light:
                    _uiServices.Set_Light_Theme();
                    break;

                default:
                    _uiServices.Set_Dark_Theme();
                    SettingsFile.AppData.ThemeName = AppTheme.Dark;
                    break;
            }
        }

        private void SetAppMode(AppMode mode)
        {
            switch (mode)
            {
                case AppMode.NoProtocol:
                    CurrentViewModel = _noProtocol_VM;
                    Model.SetProtocol_NoProtocol();
                    CurrentApplicationWorkMode = ApplicationWorkMode.NoProtocol;
                    break;

                case AppMode.ModbusClient:
                    CurrentViewModel = _modbusClient_VM;
                    Model.SetProtocol_Modbus();
                    CurrentApplicationWorkMode = ApplicationWorkMode.ModbusClient;
                    break;

                default:
                    CurrentViewModel = _noProtocol_VM;
                    Model.SetProtocol_NoProtocol();
                    SettingsFile.AppData.SelectedMode = AppMode.NoProtocol;
                    CurrentApplicationWorkMode = ApplicationWorkMode.NoProtocol;
                    break;
            }
        }

        public async Task MainWindowLoaded()
        {
            UpdateListOfPresets();

            if (SettingsFile.AppData.CheckUpdateAfterStart)
            {
                await CheckAppUpdate();
            }
        }

        public void WindowClosing()
        {
            SettingsFile.SaveAppInfo(SettingsFile.AppData);
        }

        private async Task CheckAppUpdate()
        {
            try
            {
                var appVersion = _uiServices.GetAppVersion();

                char[] appVersion_Chars = new char[20];

                if (appVersion != null)
                {
                    LastestVersionInfo? info = await AppUpdateSystem.IsUpdateAvailable(appVersion);

                    if (info != null)
                    {
                        string downloadLink = AppUpdateSystem.GetDownloadLink(info);

                        if (!string.IsNullOrEmpty(info.Version) && SettingsFile.AppData.SkippedAppVersion != info.Version)
                        {
                            NewAppVersion = info.Version;
                            _newAppDownloadLink = downloadLink;
                            UpdateMessageIsVisible = true;
                            return;
                        }
                    }
                }                

                UpdateMessageIsVisible = false;
            }
            
            catch (Exception)
            {
                UpdateMessageIsVisible = false;
            }
        } 

        private string GetConnectionString()
        {
            if (SettingsFile.Settings == null)
            {
                throw new Exception("Настройки не инициализированы.");
            }

            DeviceData settings = (DeviceData)SettingsFile.Settings.Clone();

            string separator = " : ";

            string connectionString;

            switch (settings.TypeOfConnection)
            {
                case DeviceData.ConnectionName_SerialPort:

                    if (settings.Connection_SerialPort != null)
                    {
                        if (settings.Connection_SerialPort.BaudRate_IsCustom == false &&
                             string.IsNullOrEmpty(settings.Connection_SerialPort.BaudRate) ||
                            settings.Connection_SerialPort.BaudRate_IsCustom == true &&
                             string.IsNullOrEmpty(settings.Connection_SerialPort.BaudRate_Custom) ||
                            string.IsNullOrEmpty(settings.Connection_SerialPort.Parity) ||
                            string.IsNullOrEmpty(settings.Connection_SerialPort.DataBits) ||
                            string.IsNullOrEmpty(settings.Connection_SerialPort.StopBits))
                        {
                            connectionString = "Не заданы настройки для последовательного порта";
                        }

                        else
                        {
                            connectionString =
                                (string.IsNullOrEmpty(settings.Connection_SerialPort.Port) ? "Порт не задан" : settings.Connection_SerialPort.Port) +
                                separator +
                                (settings.Connection_SerialPort.BaudRate_IsCustom == true ?
                                    settings.Connection_SerialPort.BaudRate_Custom : settings.Connection_SerialPort.BaudRate) +
                                separator +
                                settings.Connection_SerialPort.Parity +
                                separator +
                                settings.Connection_SerialPort.DataBits +
                                separator +
                                settings.Connection_SerialPort.StopBits;
                        }
                    }

                    else
                    {
                        connectionString = "Настройки не заданы";
                    }

                    break;

                case DeviceData.ConnectionName_Ethernet:

                    if (settings.Connection_IP != null)
                    {
                        connectionString =
                            (string.IsNullOrEmpty(settings.Connection_IP.IP_Address) ? "IP-адрес не задан" : settings.Connection_IP.IP_Address) +
                            separator +
                            (string.IsNullOrEmpty(settings.Connection_IP.Port) ? "Порт не задан" : settings.Connection_IP.Port);
                    }

                    else
                    {
                        connectionString = "Настройки не заданы";
                    }

                    break;

                default:
                    throw new Exception("Задан неизвестный тип подключения: " + settings.TypeOfConnection);
            }

            return connectionString;
        }

        private void ConnectionTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            _connectionTime = _connectionTime.Add(new TimeSpan(0, 0, 0, 0, ConnectionTimer_Interval_ms));

            ConnectionTimer_View = string.Format("{0:D2} : {1:D2} : {2:D2}",
                _connectionTime.Days * 24 + _connectionTime.Hours,
                _connectionTime.Minutes,
                _connectionTime.Seconds);
        }

        private void Model_DeviceIsConnect(object? sender, IConnection? e)
        {
            if (Model.Client != null)
            {
                Model.Client.Notifications.TX_Notification += Notifications_TX_Notification;
                Model.Client.Notifications.RX_Notification += Notifications_RX_Notification;

                // Использовать для демонстрации работы уведомлений приема и передачи.
                //Task.Run(Model.Client.Notifications.DemoVisualization);
            }

            UI_IsConnectedState = true;

            ConnectionStatus = ConnectionStatus_Connected;

            ConnectionTimer_IsVisible = true;

            _connectionTime = new TimeSpan();

            ConnectionTimer_View = string.Format("{0:D2} : {1:D2} : {2:D2}",
                _connectionTime.Days * 24 + _connectionTime.Hours,
                _connectionTime.Minutes,
                _connectionTime.Seconds);

            _connectionTimer.Start();
        }

        private void Notifications_TX_Notification(object? sender, bool e)
        {
            lock (TX_View_Locker)
            {
                Led_TX_IsActive = e;
            }
        }

        private void Notifications_RX_Notification(object? sender, bool e)
        {
            lock (RX_View_Locker)
            {
                Led_RX_IsActive = e;
            }
        }

        private void Model_DeviceIsDisconnected(object? sender, IConnection? e)
        {
            _connectionTimer.Stop();

            if (Model.Client != null)
            {
                Model.Client.Notifications.TX_Notification -= Notifications_TX_Notification;
                Model.Client.Notifications.RX_Notification -= Notifications_RX_Notification;
            }

            UI_IsConnectedState = false;

            ConnectionStatus = ConnectionStatus_Disconnected;

            ConnectionTimer_IsVisible = false;
        }

        private void UpdateListOfPresets()
        {
            string[] fileNames = SettingsFile.FindFilesOfPresets();

            Presets.Clear();

            foreach (string element in fileNames)
            {
                Presets.Add(element);
            }

            if (SettingsDocument == null || Presets.Contains(SettingsDocument) == false)
            {
                SettingsDocument = Presets.First();
            }

            SelectedPreset = Presets.Single(x => x == SettingsDocument);

            Led_TX_IsActive = false;
            Led_RX_IsActive = false;
        }

        private void Connect_Handler()
        {
            if (SettingsFile.Settings == null)
            {
                throw new Exception("Настройки не инициализированы.");
            }

            DeviceData settings = (DeviceData)SettingsFile.Settings.Clone();

            ConnectionInfo info;

            switch (settings.TypeOfConnection)
            {
                case DeviceData.ConnectionName_SerialPort:

                    info = new ConnectionInfo(new SerialPortInfo(
                        settings.Connection_SerialPort?.Port,
                        settings.Connection_SerialPort?.BaudRate_IsCustom == true ?
                            settings.Connection_SerialPort?.BaudRate_Custom : settings.Connection_SerialPort?.BaudRate,
                        settings.Connection_SerialPort?.Parity,
                        settings.Connection_SerialPort?.DataBits,
                        settings.Connection_SerialPort?.StopBits
                        ),
                        AppEncoding.GetEncoding(settings.GlobalEncoding));

                    break;

                case DeviceData.ConnectionName_Ethernet:

                    info = new ConnectionInfo(new SocketInfo(
                        settings.Connection_IP?.IP_Address,
                        settings.Connection_IP?.Port
                        ),
                        AppEncoding.GetEncoding(settings.GlobalEncoding));

                    break;

                default:
                    throw new Exception("В файле настроек задан неизвестный интерфейс связи.");
            }

            Model.Connect(info);
        }
    }
}
