using Core.Models.Modbus.DataTypes;
using Core.Models.Settings;
using Core.Models.Settings.DataTypes;
using Core.Models.Settings.FileTypes;
using MessageBox.Core;
using MessageBusTypes.Macros;
using MessageBusTypes.ModbusClient;
using MessageBusTypes.NoProtocol;
using ReactiveUI;
using Services.Interfaces;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using ViewModels.Helpers;
using ViewModels.Macros.DataTypes;
using ViewModels.Macros.MacrosEdit.CommandEdit;

namespace ViewModels.Macros.MacrosEdit;

// Объект создается как временный, поэтому нужно отписываться от событий MessageBus.
public class EditMacros_VM : ReactiveObject, IDisposable
{
    private const string SenderName = "EditMacros";

    private string? _macrosName = string.Empty;

    public string? MacrosName
    {
        get => _macrosName;
        set => this.RaiseAndSetIfChanged(ref _macrosName, value);
    }

    private ObservableCollection<MacrosCommandItem_VM> _commandItems = new ObservableCollection<MacrosCommandItem_VM>();

    public ObservableCollection<MacrosCommandItem_VM> CommandItems
    {
        get => _commandItems;
        set => this.RaiseAndSetIfChanged(ref _commandItems, value);
    }

    private bool _isEdit;

    public bool IsEdit
    {
        get => _isEdit;
        set => this.RaiseAndSetIfChanged(ref _isEdit, value);
    }

    private CommonSlaveIdField_VM? _commonSlaveIdFieldViewModel;

    public CommonSlaveIdField_VM? CommonSlaveIdFieldViewModel
    {
        get => _commonSlaveIdFieldViewModel;
        set => this.RaiseAndSetIfChanged(ref _commonSlaveIdFieldViewModel, value);
    }

    private object? _editCommandViewModel;

    public object? EditCommandViewModel
    {
        get => _editCommandViewModel;
        set => this.RaiseAndSetIfChanged(ref _editCommandViewModel, value);
    }

    public string EmptyCommandMessage => "Выберите команду для редактирования";

    public ReactiveCommand<Unit, Unit> Command_SaveMacros { get; }
    public ReactiveCommand<Unit, Unit> Command_RunMacros { get; }
    public ReactiveCommand<Unit, Unit> Command_AddCommand { get; }

    public bool Saved { get; private set; } = false;

    private const string _validationMessageSeparator = "\n\n---------------------------\n\n";

    private readonly List<ICommandContent> _allEditCommandVM = new List<ICommandContent>();

    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private readonly IMessageBox _messageBox;
    private readonly Model_Settings _settingsModel;


    public EditMacros_VM(IMessageBoxEditMacros messageBox, Model_Settings settingsModel)
    {
        _messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));
        _settingsModel = settingsModel ?? throw new ArgumentNullException(nameof(settingsModel));

        /****************************************************/
        //
        // Настройка прослушивания MessageBus
        //
        /****************************************************/

        MessageBus.Current.Listen<MacrosActionResponse>()
            .Subscribe(response =>
            {
                if (!response.ActionSuccess && response.Sender == SenderName)
                {
                    _messageBox.Show(response.Message ?? string.Empty, response.Type, response.Error);
                }
            })
            .DisposeWith(_disposables);

        /****************************************************/
        //
        // Настройка свойств и команд модели отображения
        //
        /****************************************************/

        Command_SaveMacros = ReactiveCommand.Create(() =>
        {
            if (string.IsNullOrWhiteSpace(MacrosName))
            {
                _messageBox.Show("Задайте имя макроса.", MessageType.Warning);
                return;
            }

            string? validationMessages = GetMacrosValidationMessage();

            if (!string.IsNullOrEmpty(validationMessages))
            {
                _messageBox.Show($"Исправьте ошибки в макросе.{_validationMessageSeparator}{validationMessages}", MessageType.Error);
                return;
            }

            Saved = true;

            _messageBox.Show("Настройки макроса сохранены!", MessageType.Information);
        });
        Command_SaveMacros.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка сохранения макроса.\n\n{error.Message}", MessageType.Error, error));

        Command_RunMacros = ReactiveCommand.Create(RunMacros);
        Command_RunMacros.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка запуска макроса.\n\n{error.Message}", MessageType.Error, error));

        Command_AddCommand = ReactiveCommand.Create(() =>
        {
            string defaultName = (CommandItems.Count() + 1).ToString();

            var commandParameters = new EditCommandParameters(defaultName, null);

            var itemGuid = Guid.NewGuid();

            CommandItems.Add(new MacrosCommandItem_VM(itemGuid, commandParameters, RunCommand, EditCommand, RemoveCommand, _messageBox));
            _allEditCommandVM.Add(CreateCommandVM(itemGuid, commandParameters));
        });
        Command_AddCommand.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка добавления команды.\n\n{error.Message}", MessageType.Error, error));

        this.WhenAnyValue(x => x.EditCommandViewModel)
            .Subscribe(x =>
            {
                IsEdit = x != null ? true : false;
            });

        if (MainWindow_VM.CurrentApplicationWorkMode == ApplicationWorkMode.ModbusClient)
        {
            CommonSlaveIdFieldViewModel = new CommonSlaveIdField_VM(messageBox);

            CommonSlaveIdFieldViewModel.UseCommonSlaveIdChanged += CommonSlaveIDFieldViewModel_UseCommonSlaveIdChanged;
        }
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    private void CommonSlaveIDFieldViewModel_UseCommonSlaveIdChanged(object? sender, bool e)
    {
        UseCommonSlaveId(e);
    }

    private void UseCommonSlaveId(bool value)
    {
        foreach (var commandVM in _allEditCommandVM.OfType<ModbusCommand_VM>())
        {
            commandVM.UseCommonSlaveId = value;
        }
    }

    public void SetParameters(object? macrosParameters)
    {
        IEnumerable<EditCommandParameters>? commands;

        bool isCommonSlaveId = false;

        if (macrosParameters is MacrosContent<object, MacrosCommandNoProtocol> noProtocolContent)
        {
            MacrosName = noProtocolContent.MacrosName;
            commands = noProtocolContent.Commands?.Select(e => new EditCommandParameters(e.Name, e));
        }

        else if (macrosParameters is MacrosContent<ModbusAdditionalData, MacrosCommandModbus> modbusContent)
        {
            MacrosName = modbusContent.MacrosName;
            commands = modbusContent.Commands?.Select(e => new EditCommandParameters(e.Name, e));

            if (CommonSlaveIdFieldViewModel != null && modbusContent.AdditionalData != null)
            {
                CommonSlaveIdFieldViewModel.UseCommonSlaveId = modbusContent.AdditionalData.UseCommonSlaveId;
                CommonSlaveIdFieldViewModel.SetSlaveId(modbusContent.AdditionalData.CommonSlaveId);

                isCommonSlaveId = modbusContent.AdditionalData.UseCommonSlaveId;
            }
        }

        else
        {
            throw new NotImplementedException();
        }

        if (commands != null)
        {
            foreach (var commandParameters in commands)
            {
                var itemGuid = Guid.NewGuid();

                CommandItems.Add(new MacrosCommandItem_VM(itemGuid, commandParameters, RunCommand, EditCommand, RemoveCommand, _messageBox));
                _allEditCommandVM.Add(CreateCommandVM(itemGuid, commandParameters));
            }

            UseCommonSlaveId(isCommonSlaveId);
        }
    }

    public object GetMacrosContent()
    {
        switch (MainWindow_VM.CurrentApplicationWorkMode)
        {
            case ApplicationWorkMode.NoProtocol:
                return GetNoProtocolMacrosContent();

            case ApplicationWorkMode.ModbusClient:
                return GetModbusMacrosContent();

            default:
                throw new NotImplementedException();
        }
    }

    private ICommandContent CreateCommandVM(Guid id, EditCommandParameters parameters)
    {
        switch (MainWindow_VM.CurrentApplicationWorkMode)
        {
            case ApplicationWorkMode.NoProtocol:
                return new NoProtocolCommand_VM(id, parameters);

            case ApplicationWorkMode.ModbusClient:
                return new ModbusCommand_VM(id, parameters, _messageBox, _settingsModel, CommonSlaveIdFieldViewModel?.UseCommonSlaveId ?? false);

            default:
                throw new NotImplementedException();
        }
    }

    private void RunMacros()
    {
        string? validationMessages = GetMacrosValidationMessage();

        if (!string.IsNullOrEmpty(validationMessages))
        {
            _messageBox.Show(validationMessages, MessageType.Error);
            return;
        }

        switch (MainWindow_VM.CurrentApplicationWorkMode)
        {
            case ApplicationWorkMode.NoProtocol:
                SendNoProtocolMacros();
                break;

            case ApplicationWorkMode.ModbusClient:
                SendModbusMacros();
                break;

            default:
                throw new NotImplementedException();
        }
    }

    private void SendNoProtocolMacros()
    {
        var noProtocolContent = GetNoProtocolMacrosContent();

        noProtocolContent.Sender = SenderName;

        MessageBus.Current.SendMessage(noProtocolContent);
    }

    private void SendModbusMacros()
    {
        var modbusContent = GetModbusMacrosContent();

        var contentForSend = MacrosHelper.GetWithAdditionalData(modbusContent);

        contentForSend.Sender = SenderName;

        MessageBus.Current.SendMessage(contentForSend);
    }

    private MacrosContent<object, MacrosCommandNoProtocol> GetNoProtocolMacrosContent()
    {
        var content = new MacrosContent<object, MacrosCommandNoProtocol>();

        content.MacrosName = MacrosName;
        content.Commands = new List<MacrosCommandNoProtocol>();

        content.Commands = _allEditCommandVM
            .Select(e =>
            {
                object content = e.GetContent();

                if (content is MacrosCommandNoProtocol data && data.Content != null)
                {
                    return data;
                }

                return new MacrosCommandNoProtocol()
                {
                    Name = e.Name,
                    Content = null,
                };
            })
            .ToList();

        return content;
    }

    private MacrosContent<ModbusAdditionalData, MacrosCommandModbus> GetModbusMacrosContent()
    {
        var content = new MacrosContent<ModbusAdditionalData, MacrosCommandModbus>();

        ModbusAdditionalData? additionalData = CommonSlaveIdFieldViewModel?.GetAdditionalData();

        content.MacrosName = MacrosName;
        content.AdditionalData = additionalData;
        content.Commands = new List<MacrosCommandModbus>();

        content.Commands = _allEditCommandVM
            .Select(e =>
            {
                object content = e.GetContent();

                if (content is MacrosCommandModbus data && data.Content != null)
                {
                    return data;
                }

                return new MacrosCommandModbus()
                {
                    Name = e.Name,
                    Content = null,
                };
            })
            .ToList();

        return content;
    }

    private void EditCommand(Guid selectedId)
    {
        var commandItem = CommandItems.First(e => e.Id == selectedId);
        var commandVM = _allEditCommandVM.First(e => e.Id == selectedId);

        // Перед каждой сменой VM нужно обнулять ссылку, иначе будет некорректное поведение RadioButton,
        // а также, возможно, и других элементов с типом привязки TwoWay.
        EditCommandViewModel = null;

        bool currentItemIsEdit = commandItem.IsEdit;

        // Снимаем выделение у всех команд.
        foreach (var item in CommandItems)
        {
            if (item.IsEdit)
            {
                item.CommandName = _allEditCommandVM.First(e => e.Id == item.Id).Name;
            }

            item.IsEdit = false;
        }

        if (currentItemIsEdit)
        {
            return;
        }

        commandItem.IsEdit = true;
        EditCommandViewModel = commandVM;
    }

    private void RemoveCommand(Guid selectedId)
    {
        var commandItem = CommandItems.First(e => e.Id == selectedId);
        var commandVM = _allEditCommandVM.First(e => e.Id == selectedId);

        if (commandItem.IsEdit)
        {
            EditCommandViewModel = null;
        }

        CommandItems.Remove(commandItem);
        _allEditCommandVM.Remove(commandVM);
    }

    private void RunCommand(Guid selectedId)
    {
        var commandVM = _allEditCommandVM.First(e => e.Id == selectedId);

        string? validationMessage = GetCommandValidationMessage(commandVM);

        if (!string.IsNullOrEmpty(validationMessage))
        {
            _messageBox.Show(validationMessage, MessageType.Error);
            return;
        }

        object content = commandVM.GetContent();

        if (content is MacrosCommandNoProtocol noProtocolData && noProtocolData.Content != null)
        {
            RunNoProtocolCommand(noProtocolData.Content);
        }

        else if (content is MacrosCommandModbus modbusData && modbusData.Content != null)
        {
            RunModbusCommand(modbusData.Content);
        }
    }

    private void RunNoProtocolCommand(NoProtocolCommandContent content)
    {
        MessageBus.Current.SendMessage(
            new NoProtocolSendMessage(SenderName, content.IsByteString, content.Message, content.EnableCR, content.EnableLF, AppEncoding.GetEncoding(content.MacrosEncoding))
            );
    }

    private void RunModbusCommand(ModbusCommandContent commandContent)
    {
        var simpleMacros = new MacrosContent<ModbusAdditionalData, MacrosCommandModbus>()
        {
            AdditionalData = CommonSlaveIdFieldViewModel?.GetAdditionalData(),
            Commands = new List<MacrosCommandModbus>
            {
                new MacrosCommandModbus()
                {
                    Name = "Одиночная команда",
                    Content = commandContent
                }
            }
        };

        var contentForSend = MacrosHelper.GetWithAdditionalData(simpleMacros);

        var content = contentForSend.Commands?.First().Content;

        if (content == null)
            return;

        var selectedFunction = Function.AllFunctions.First(func => func.Number == content.FunctionNumber);

        if (selectedFunction is ModbusReadFunction readFunction)
        {
            MessageBus.Current.SendMessage(
                new ModbusReadMessage(
                    SenderName, 
                    content.SlaveID, 
                    content.Address, 
                    readFunction, 
                    content.NumberOfReadRegisters, 
                    content.CheckSum_IsEnable
                    ));

            return;
        }

        if (selectedFunction is ModbusWriteFunction writeFunction)
        {
            MessageBus.Current.SendMessage(
                new ModbusWriteMessage(
                    SenderName, 
                    content.SlaveID, 
                    content.Address, 
                    writeFunction, 
                    content.WriteInfo?.WriteBuffer,
                    content.WriteInfo != null ? content.WriteInfo.NumberOfWriteRegisters : 0,
                    content.CheckSum_IsEnable)
                );

            return;
        }

        throw new Exception($"Задан неизвестный тип функции Modbus.\nКод: {content.FunctionNumber}");
    }

    private string? GetMacrosValidationMessage()
    {
        var validationMessages = new List<string>();

        if (CommonSlaveIdFieldViewModel != null)
        {
            string? message = CommonSlaveIdFieldViewModel.GetErrorMessage();

            if (!string.IsNullOrEmpty(message))
            {
                validationMessages.Add(message);
            }
        }

        foreach (var command in _allEditCommandVM)
        {
            string? message = GetCommandValidationMessage(command);

            if (!string.IsNullOrEmpty(message))
            {
                validationMessages.Add(message);
            }
        }

        if (validationMessages.Any())
        {
            return string.Join(_validationMessageSeparator, validationMessages);
        }

        return null;
    }

    private string? GetCommandValidationMessage(ICommandContent command)
    {
        if (command is ICommandValidation validatedCommand)
        {
            var uncheckedFields = CommonSlaveIdFieldViewModel?.UseCommonSlaveId == true ? [ FieldNames.SlaveID ] : Array.Empty<FieldNames>();

            string? message = validatedCommand.GetValidationMessage(uncheckedFields);

            if (!string.IsNullOrEmpty(message))
            {
                return $"Ошибка в команде \"{command.Name}\".\n\n{message}";
            }
        }

        return null;
    }
}
