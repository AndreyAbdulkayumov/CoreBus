using Core.Models.Settings;
using Core.Models.Settings.DataTypes;
using Core.Models.Settings.FileTypes;
using MessageBox.Core;
using MessageBusTypes.Macros;
using ReactiveUI;
using Services.Interfaces;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ViewModels.Macros.DataTypes;

namespace ViewModels.Macros;

// Объект создается как временный, поэтому нужно отписываться от событий MessageBus.
public class Macros_VM : ReactiveObject, IDisposable
{
    private const string SenderName = "MacrosWorkspace";

    private const string ModeName_NoProtocol = "Без протокола";
    private const string ModeName_ModbusClient = "Modbus";

    private bool _windowIsVisible = true;

    public bool WindowIsVisible
    {
        get => _windowIsVisible;
        set => this.RaiseAndSetIfChanged(ref _windowIsVisible, value);
    }

    private string? _modeName;

    public string? ModeName
    {
        get => _modeName;
        set => this.RaiseAndSetIfChanged(ref _modeName, value);
    }

    private ObservableCollection<MacrosViewItem_VM> _items = new ObservableCollection<MacrosViewItem_VM>();

    public ObservableCollection<MacrosViewItem_VM> Items
    {
        get => _items;
        set => this.RaiseAndSetIfChanged(ref _items, value);
    }

    public ReactiveCommand<Unit, Unit> Command_Import { get; }
    public ReactiveCommand<Unit, Unit> Command_Export { get; }
    public ReactiveCommand<Unit, Unit> Command_CreateMacros { get; }

    private MacrosNoProtocol? _noProtocolMacros;
    private MacrosModbus? _modbusMacros;

    private List<string> _allMacrosNames = new List<string>();
        
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private readonly IOpenChildWindowService _openChildWindowService;
    private readonly IFileSystemService _fileSystemService;
    private readonly IMessageBox _messageBox;
    private readonly Model_Settings _settingsModel;

    public Macros_VM(IOpenChildWindowService openChildWindow, IFileSystemService fileSystemService, IMessageBoxMacros messageBox,
        Model_Settings settingsModel)
    {
        _openChildWindowService = openChildWindow ?? throw new ArgumentNullException(nameof(openChildWindow));
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        _messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));
        _settingsModel = settingsModel ?? throw new ArgumentNullException(nameof(settingsModel));

        /****************************************************/
        //
        // Настройка прослушивания MessageBus
        //
        /****************************************************/

        _disposables.Add(
            MessageBus.Current.Listen<MacrosActionResponse>()
                .Subscribe(response =>
                {
                    if (!response.ActionSuccess && response.Sender == SenderName)
                    {
                        _messageBox.Show(response.Message ?? string.Empty, response.Type, response.Error);
                    }
                }));

        /****************************************************/
        //
        // Настройка свойств и команд модели отображения
        //
        /****************************************************/

        Command_Import = ReactiveCommand.CreateFromTask(ImportMacros);
        Command_Import.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка при импорте макросов.\n\n{error.Message}", MessageType.Error, error));

        Command_Export = ReactiveCommand.CreateFromTask(ExportMacros);
        Command_Export.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка при экспорте макроса.\n\n{error.Message}", MessageType.Error, error));

        Command_CreateMacros = ReactiveCommand.CreateFromTask(CreateMacros);
        Command_CreateMacros.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка при создании макроса.\n\n{error.Message}", MessageType.Error, error));

        MainWindow_VM.ApplicationWorkModeChanged += CommonUI_VM_ApplicationWorkModeChanged;

        InitUI();
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    private void InitUI()
    {
        ModeName = GetModeName(MainWindow_VM.CurrentApplicationWorkMode);
        UpdateWorkspace(MainWindow_VM.CurrentApplicationWorkMode);
    }

    private string GetValidMacrosFileName()
    {
        if (_noProtocolMacros != null)
            return _settingsModel.FilePath_Macros_NoProtocol;

        if (_modbusMacros != null)
            return _settingsModel.FilePath_Macros_Modbus;

        throw new Exception("Не выбран режим.");
    }

    private string GetModeName(ApplicationWorkMode mode)
    {
        switch (mode)
        {
            case ApplicationWorkMode.NoProtocol:
                return ModeName_NoProtocol;

            case ApplicationWorkMode.ModbusClient:
                return ModeName = ModeName_ModbusClient;

            default:
                throw new NotImplementedException();
        }
    }

    private void CommonUI_VM_ApplicationWorkModeChanged(object? sender, ApplicationWorkMode e)
    {
        ModeName = GetModeName(e);

        UpdateWorkspace(e);
    }

    private void UpdateWorkspace(ApplicationWorkMode mode)
    {
        Items.Clear();

        _noProtocolMacros = null;
        _modbusMacros = null;

        _allMacrosNames.Clear();

        switch (mode)
        {
            case ApplicationWorkMode.NoProtocol:
                _noProtocolMacros = BuildNoProtocolMacros();
                break;

            case ApplicationWorkMode.ModbusClient:
                _modbusMacros = BuildModbusMacros();
                break;

            default:
                throw new NotImplementedException();
        }
    }

    private MacrosNoProtocol BuildNoProtocolMacros()
    {
        MacrosNoProtocol macros = _settingsModel.ReadOrCreateDefaultMacros<MacrosNoProtocol>();

        if (macros.Items == null)
        {
            return new MacrosNoProtocol()
            {
                Items = new List<MacrosContent<object, MacrosCommandNoProtocol>>()
            };
        }

        foreach (var element in macros.Items)
        {
            IMacrosContext _macrosContext = new ViewItemContext<object, MacrosCommandNoProtocol>(element, SenderName);

            BuildMacrosItem(_macrosContext.CreateContext());
        }

        return macros;
    }

    private MacrosModbus BuildModbusMacros()
    {
        MacrosModbus macros = _settingsModel.ReadOrCreateDefaultMacros<MacrosModbus>();

        if (macros.Items == null)
        {
            return new MacrosModbus()
            {
                Items = new List<MacrosContent<ModbusAdditionalData, MacrosCommandModbus>>()
            };
        }

        foreach (var element in macros.Items)
        {
            IMacrosContext _macrosContext = new ViewItemContext<ModbusAdditionalData, MacrosCommandModbus>(element, SenderName);

            BuildMacrosItem(_macrosContext.CreateContext());
        }

        return macros;
    }

    private async Task<object?> OpenEditMacrosWindow(object? parameters)
    {
        try
        {
            WindowIsVisible = false;

            return await _openChildWindowService.EditMacros(parameters);
        }

        finally
        {
            WindowIsVisible = true;
        }
    }

    private async Task CreateMacros()
    {
        var currentMode = MainWindow_VM.CurrentApplicationWorkMode;

        var content = await OpenEditMacrosWindow(null);

        if (content == null)
        {
            return;
        }

        AddMacrosItem(content);

        SaveMacros(currentMode);

        // На случай если режим будет изменен во время создания нового макроса
        if (currentMode.Equals(MainWindow_VM.CurrentApplicationWorkMode))
        {
            BuildMacrosItem(GetMacrosContextFrom(content).CreateContext());
        }
    }

    private async Task EditMacros(string name)
    {
        var currentMode = MainWindow_VM.CurrentApplicationWorkMode;

        object? initData;

        switch (currentMode)
        {
            case ApplicationWorkMode.NoProtocol:
                initData = _noProtocolMacros?.Items?.Find(e => e.MacrosName == name);
                break;

            case ApplicationWorkMode.ModbusClient:
                initData = _modbusMacros?.Items?.Find(e => e.MacrosName == name);
                break;

            default:
                throw new NotImplementedException();
        }

        object? content = await OpenEditMacrosWindow(initData);

        if (content == null)
        {
            return;
        }

        ChangeMacrosItem(name, content);

        SaveMacros(currentMode);

        // На случай если режим будет изменен во время создания нового макроса
        if (currentMode.Equals(MainWindow_VM.CurrentApplicationWorkMode))
        {
            ChangeMacrosViewItem(name, GetMacrosContextFrom(content).CreateContext());
        }
    }

    private void DeleteMacros(string name)
    {
        var viewItemToRemove = Items.First(item => item.Title == name);

        if (viewItemToRemove != null)
        {
            Items.Remove(viewItemToRemove);
        }

        _allMacrosNames.Remove(name);

        DeleteMacrosItem(name);

        SaveMacros(MainWindow_VM.CurrentApplicationWorkMode);
    }

    private void BuildMacrosItem(MacrosViewItemData itemData)
    {
        Items.Add(new MacrosViewItem_VM(itemData.Name, itemData.MacrosAction, EditMacros, DeleteMacros, _messageBox));
        _allMacrosNames.Add(itemData.Name);
    }

    private async Task ImportMacros()
    {
        ApplicationWorkMode workMode = MainWindow_VM.CurrentApplicationWorkMode;

        string modeName = GetModeName(workMode);

        if (await _messageBox.ShowYesNoDialog(
            $"Внимание!!!\n\n" +
            $"При импорте файла макросов для режима \"{modeName}\" старые макросы будут удалены без возможности восстановления. Продолжить?",
            MessageType.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        string? macrosFilePath = await _fileSystemService.GetFilePath($"Выбор файла для импорта макросов режима \"{modeName}\".", "Файл макросов", ["*.json"]);

        if (macrosFilePath != null)
        {
            string fileName = Path.GetFileName(macrosFilePath);

            string macrosValidFilePath = GetValidMacrosFileName();
            string validFileName = Path.GetFileName(macrosValidFilePath);

            if (fileName != validFileName)
            {
                throw new Exception($"Некорректное имя файла макроса.\nОжидается имя \"{validFileName}\".");
            }

            try
            {
                switch (workMode)
                {
                    case ApplicationWorkMode.NoProtocol:
                        var macrosNoProtocol = _settingsModel.ReadMacros<MacrosNoProtocol>(macrosFilePath);
                        break;

                    case ApplicationWorkMode.ModbusClient:
                        var macrosModbus = _settingsModel.ReadMacros<MacrosModbus>(macrosFilePath);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            catch (Exception error)
            {
                throw new Exception($"Ошибка чтения файла.\n\n{error.Message}");
            }

            _settingsModel.DeleteFile(macrosValidFilePath);

            _settingsModel.CopyFile(macrosFilePath, macrosValidFilePath);

            // На случай если режим будет изменен во время импорта
            if (workMode.Equals(MainWindow_VM.CurrentApplicationWorkMode))
            {
                UpdateWorkspace(workMode);
            }

            _messageBox.Show($"Файл с макросами для режима \"{modeName}\" успешно импортирован!", MessageType.Information);
        }
    }

    private async Task ExportMacros()
    {
        string? outputFilePath = await _fileSystemService.GetFolderPath("Выбор папки для экспорта файла макросов.");

        if (outputFilePath != null)
        {
            string macrosFileName = GetValidMacrosFileName();

            string outputFileName = Path.Combine(outputFilePath, Path.GetFileName(macrosFileName));

            _settingsModel.CopyFile(macrosFileName, outputFileName);

            _messageBox.Show($"Экспорт прошел успешно!\n\nПуть к файлу:\n{outputFileName}", MessageType.Information);
        }
    }

    private IMacrosContext GetMacrosContextFrom(object? content)
    {
        if (content is MacrosContent<object, MacrosCommandNoProtocol> noProtocolContent)
            return new ViewItemContext<object, MacrosCommandNoProtocol>(noProtocolContent, SenderName);

        if (content is MacrosContent<ModbusAdditionalData, MacrosCommandModbus> modbusContent)
            return new ViewItemContext<ModbusAdditionalData, MacrosCommandModbus>(modbusContent, SenderName);

        throw new NotImplementedException($"Поддержка режима не реализована.");
    }

    private void SaveMacros(ApplicationWorkMode mode)
    {
        switch (mode)
        {
            case ApplicationWorkMode.NoProtocol:
                _settingsModel.SaveMacros(_noProtocolMacros);
                break;

            case ApplicationWorkMode.ModbusClient:
                _settingsModel.SaveMacros(_modbusMacros);
                break;

            default:
                throw new NotImplementedException();
        }
    }

    private void AddMacrosItem(object content)
    {
        if (content is MacrosContent<object, MacrosCommandNoProtocol> noProtocolContent)
        {
            _noProtocolMacros?.Items?.Add(noProtocolContent);
        }

        else if (content is MacrosContent<ModbusAdditionalData, MacrosCommandModbus> modbusContent)
        {
            _modbusMacros?.Items?.Add(modbusContent);
        }

        else
        {
            throw new NotImplementedException();
        }
    }

    private void DeleteMacrosItem(string name)
    {
        switch (MainWindow_VM.CurrentApplicationWorkMode)
        {
            case ApplicationWorkMode.NoProtocol:

                var noProtocolItem = _noProtocolMacros?.Items?.First(macros => macros.MacrosName == name);

                if (noProtocolItem != null)
                {
                    _noProtocolMacros?.Items?.Remove(noProtocolItem);
                }

                break;

            case ApplicationWorkMode.ModbusClient:

                var modbusItem = _modbusMacros?.Items?.First(macros => macros.MacrosName == name);

                if (modbusItem != null)
                {
                    _modbusMacros?.Items?.Remove(modbusItem);
                }

                break;

            default:
                throw new NotImplementedException();
        }
    }

    private void ChangeMacrosItem(string oldName, object newContent)
    {
        if (newContent is MacrosContent<object, MacrosCommandNoProtocol> noProtocolContent)
        {
            var item = _noProtocolMacros?.Items?.First(item => item.MacrosName == oldName);

            if (item != null)
            {
                item.MacrosName = noProtocolContent.MacrosName;
                item.Commands = noProtocolContent.Commands;
            }
        }

        else if (newContent is MacrosContent<ModbusAdditionalData, MacrosCommandModbus> modbusContent)
        {
            var item = _modbusMacros?.Items?.First(item => item.MacrosName == oldName);

            if (item != null)
            {
                item.MacrosName = modbusContent.MacrosName;
                item.AdditionalData = modbusContent.AdditionalData;
                item.Commands = modbusContent.Commands;
            }
        }

        else
        {
            throw new NotImplementedException();
        }
    }

    private void ChangeMacrosViewItem(string oldName, MacrosViewItemData newData)
    {
        var viewItem = Items.First(macros => macros.Title == oldName);

        if (viewItem != null)
        {
            viewItem.Title = newData.Name;
            viewItem.ClickAction = newData.MacrosAction;

            _allMacrosNames = Items.Select(item => item.Title).ToList();
        }
    }
}
