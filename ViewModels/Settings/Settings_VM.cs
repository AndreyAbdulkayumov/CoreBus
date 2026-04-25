using System.Text;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using Core.Models.Settings;
using Core.Models.Settings.FileTypes;
using MessageBox.Core;
using ViewModels.Settings.Tabs;
using ViewModels.Validation;
using ViewModels.Helpers.FloatNumber;
using Services.Interfaces;
using MessageBusTypes.Settings;

namespace ViewModels.Settings;

public class Settings_VM : ReactiveObject
{
    private ObservableCollection<string> _presets = new ObservableCollection<string>();

    public ObservableCollection<string> Presets
    {
        get => _presets;
        set => this.RaiseAndSetIfChanged(ref _presets, value);
    }

    private string _selectedPreset = string.Empty;

    public string SelectedPreset
    {
        get => _selectedPreset;
        set => this.RaiseAndSetIfChanged(ref _selectedPreset, value);
    }

    private readonly Connection_VM _tab_Connection_VM;

    public Connection_VM Tab_Connection_VM
    {
        get => _tab_Connection_VM;
    }

    private readonly Settings_NoProtocol_VM _tab_NoProtocol_VM;

    public Settings_NoProtocol_VM Tab_NoProtocol_VM
    {
        get => _tab_NoProtocol_VM;
    }

    private readonly Modbus_VM _tab_Modbus_VM;

    public Modbus_VM Tab_Modbus_VM
    {
        get => _tab_Modbus_VM;
    }

    private readonly AppSettings_VM _tab_AppSettings_VM;

    public AppSettings_VM Tab_AppSettings_VM
    {
        get => _tab_AppSettings_VM;
    }

    public ReactiveCommand<Unit, Unit> Command_File_AddNew { get; }
    public ReactiveCommand<Unit, Unit> Command_File_AddExisting { get; }
    public ReactiveCommand<Unit, Unit> Command_File_Delete { get; }
    public ReactiveCommand<Unit, Unit> Command_File_Save { get; }

    private readonly IFileSystemService _fileSystemService;
    private readonly IOpenChildWindowService _openChildWindowService;
    private readonly IMessageBoxSettings _messageBox;
    private readonly ILocalizationService _localization;

    private readonly Model_Settings _settingsModel;

    public Settings_VM(IFileSystemService fileSystemService, IOpenChildWindowService openChildWindowService, IMessageBoxSettings messageBox,
        Model_Settings settingsModel,
        Connection_VM connectionVM, Settings_NoProtocol_VM settingsNoProtocolVM, Modbus_VM modbusVM, AppSettings_VM appSettingsVM,
        ILocalizationService localization)
    {
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        _openChildWindowService = openChildWindowService ?? throw new ArgumentNullException(nameof(openChildWindowService));
        _messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));
        _settingsModel = settingsModel ?? throw new ArgumentNullException(nameof(settingsModel));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _tab_Connection_VM = connectionVM ?? throw new ArgumentNullException(nameof(connectionVM));
        _tab_NoProtocol_VM = settingsNoProtocolVM ?? throw new ArgumentNullException(nameof(settingsNoProtocolVM));
        _tab_Modbus_VM = modbusVM ?? throw new ArgumentNullException(nameof(modbusVM));
        _tab_AppSettings_VM = appSettingsVM ?? throw new ArgumentNullException(nameof(appSettingsVM));

        Command_File_AddNew = ReactiveCommand.CreateFromTask(File_CreateNew_Handler);
        Command_File_AddExisting = ReactiveCommand.CreateFromTask(File_AddExisting_Handler);
        Command_File_Delete = ReactiveCommand.CreateFromTask(File_Delete_Handler);
        Command_File_Save = ReactiveCommand.Create(File_Save_Handler);

        this.WhenAnyValue(x => x.SelectedPreset)
            .WhereNotNull()
            .Where(x => !string.IsNullOrEmpty(x))
            .Subscribe(UpdateUI);
    }

    private void UpdateUI(string fileName)
    {
        DeviceData settings = _settingsModel.ReadPreset(fileName);

        Tab_NoProtocol_VM.SelectedEncoding = settings.GlobalEncoding ?? DeviceData.GlobalEncoding_Default;

        Tab_Modbus_VM.WriteTimeout = settings.TimeoutWrite ?? DeviceData.TimeoutWrite_Default;
        Tab_Modbus_VM.ReadTimeout = settings.TimeoutRead ?? DeviceData.TimeoutRead_Default;

        Tab_Modbus_VM.PresetLogTimestampFormat(settings.LogTimestampFormat);

        Tab_Modbus_VM.FloatFormat = FloatHelper.GetFloatNumberFormatOrDefault(settings.FloatNumberFormat);

        switch (settings.TypeOfConnection)
        {
            case DeviceData.ConnectionName_SerialPort:
                Tab_Connection_VM.Selected_SerialPort = true;
                break;

            case DeviceData.ConnectionName_Ethernet:
                Tab_Connection_VM.Selected_Ethernet = true;
                break;
        }

        Tab_Connection_VM.SettingsFileChanged();
    }

    public void WindowLoaded()
    {
        UpdateListOfPresets();

        SelectedPreset = Presets.Single(x => x == MainWindow_VM.SettingsDocument);
    }

    public void WindowClosed()
    {
        MessageBus.Current.SendMessage(new PresetUpdateTriggerMessage());
    }

    public void Enter_KeyDownHandler()
    {
        File_Save_Handler();
    }

    private void UpdateListOfPresets()
    {
        string[] fileNames = _settingsModel.FindFilesOfPresets();

        Presets.Clear();

        foreach (string element in fileNames)
        {
            Presets.Add(element);
        }
    }

    private async Task File_CreateNew_Handler()
    {
        string? fileName = await _openChildWindowService.UserInput();

        if (!string.IsNullOrEmpty(fileName))
        {
            _settingsModel.SavePreset(fileName, DeviceData.GetDefault());

            UpdateListOfPresets();

            SelectedPreset = Presets.Single(x => x == fileName);
        }
    }

    private async Task File_AddExisting_Handler()
    {
        try
        {
            string? filePath = await _fileSystemService.GetFilePath(_localization.Get("Settings.AddExistingFileTitle"), _localization.Get("Settings.FileTypeLabel"), ["*.json"]);

            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            string fileName = _settingsModel.CopyInPresetFolderFrom(filePath);

            UpdateListOfPresets();

            SelectedPreset = Presets.Single(x => x == fileName);
        }

        catch (Exception error)
        {
            _messageBox.Show(_localization.Get("Error.AddExistingSettingsFile") + "\n\n" + error.Message, MessageType.Error, error);
        }
    }

    private async Task File_Delete_Handler()
    {
        try
        {
            if (Presets.Count <= 1)
            {
                _messageBox.Show(_localization.Get("Warning.CannotDeleteLastFile"), MessageType.Warning);
                return;
            }

            MessageBoxResult dialogResult = await _messageBox.ShowYesNoDialog(_localization.Get("Confirm.DeletePresetFile", SelectedPreset), MessageType.Warning);

            if (dialogResult != MessageBoxResult.Yes)
            {
                return;
            }

            _settingsModel.DeletePreset(SelectedPreset);

            UpdateListOfPresets();

            SelectedPreset = Presets.First();
        }

        catch (Exception error)
        {
            _messageBox.Show(_localization.Get("Error.DeleteSettingsFile") + "\n\n" + error.Message, MessageType.Error, error);
        }
    }

    private void File_Save_Handler()
    {
        try
        {
            string? validationMessage = CheckTabFields();

            if (!string.IsNullOrEmpty(validationMessage))
            {
                _messageBox.Show(validationMessage, MessageType.Warning);
                return;
            }

            string floatFormat = string.Empty;

            switch (Tab_Modbus_VM.FloatFormat)
            {
                case FloatNumberFormat.AB_CD:
                    floatFormat = DeviceData.FloatWriteFormat_AB_CD;
                    break;

                case FloatNumberFormat.BA_DC:
                    floatFormat = DeviceData.FloatWriteFormat_BA_DC;
                    break;

                case FloatNumberFormat.CD_AB:
                    floatFormat = DeviceData.FloatWriteFormat_CD_AB;
                    break;

                case FloatNumberFormat.DC_BA:
                    floatFormat = DeviceData.FloatWriteFormat_DC_BA;
                    break;
            }

            var connectionType = Tab_Connection_VM.Selected_SerialPort ? DeviceData.ConnectionName_SerialPort : DeviceData.ConnectionName_Ethernet;

            var SerialPortSettings = _tab_Connection_VM.GetSerialPortClientSettings();
            var IpSettings = _tab_Connection_VM.GetIpClientSettings();

            var data = new DeviceData()
            {
                TypeOfConnection = connectionType,

                Connection_SerialPort = SerialPortSettings,

                Connection_IP = IpSettings,

                GlobalEncoding = Tab_NoProtocol_VM.SelectedEncoding,

                TimeoutWrite = Tab_Modbus_VM.WriteTimeout,
                TimeoutRead = Tab_Modbus_VM.ReadTimeout,

                LogTimestampFormat = Tab_Modbus_VM.GetLogTimestampFormat(),

                FloatNumberFormat = floatFormat,
            };

            _settingsModel.SavePreset(SelectedPreset, data);

            MainWindow_VM.SettingsDocument = SelectedPreset;

            _tab_Connection_VM.PresetSaveHandler(data.Connection_SerialPort);

            _messageBox.Show(_localization.Get("Info.SettingsSaved"), MessageType.Information);
        }

        catch (Exception error)
        {
            _messageBox.Show(_localization.Get("Error.SaveSettingsFile") + "\n\n" + error.Message, MessageType.Error, error);
        }
    }

    private string? CheckTabFields()
    {
        StringBuilder message = new StringBuilder();

        IEnumerable<ReactiveObject> neededTabs = [
            Tab_Connection_VM.GetActiveTab(),
                Tab_NoProtocol_VM,
                Tab_Modbus_VM,
                Tab_AppSettings_VM
        ];

        foreach (var tab in neededTabs)
        {
            var validationTab = tab as ValidatedDateInput;

            if (validationTab != null && validationTab.HasErrors)
            {
                foreach (KeyValuePair<string, ValidateMessage> element in validationTab.ActualErrors)
                {
                    message.AppendLine($"[{(tab as IValidationFieldInfo)?.GetFieldViewName(element.Key)}]\n{validationTab.GetFullErrorMessage(element.Key)}\n");
                }
            }
        }

        if (message.Length > 0)
        {
            message.Insert(0, _localization.Get("Validation.ErrorsHeader") + "\n\n");
            return message.ToString().TrimEnd('\r', '\n');
        }

        return null;
    }
}
