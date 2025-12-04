using Core.Clients.DataTypes;
using Core.Models;
using Core.Models.Modbus.DataTypes;
using Core.Models.Settings;
using MessageBox.Core;
using MessageBusTypes.ModbusClient;
using ReactiveUI;
using Services.Interfaces;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using ViewModels.ModbusClient.Manual.DataTypes;
using ViewModels.ModbusClient.Manual.WriteFields;
using ViewModels.ModbusClient.Manual.WriteFields.DataTypes;
using ViewModels.Validation;

namespace ViewModels.ModbusClient.Manual;

public class RequestBuilder_VM : ValidatedDateInput, IValidationFieldInfo
{
    private bool ui_IsEnable = false;

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

    private bool _checkSum_IsEnable;

    public bool CheckSum_IsEnable
    {
        get => _checkSum_IsEnable;
        set => this.RaiseAndSetIfChanged(ref _checkSum_IsEnable, value);
    }

    private bool _checkSum_IsVisible;

    public bool CheckSum_IsVisible
    {
        get => _checkSum_IsVisible;
        set => this.RaiseAndSetIfChanged(ref _checkSum_IsVisible, value);
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

    private string? _address;

    public string? Address
    {
        get => _address;
        set
        {
            this.RaiseAndSetIfChanged(ref _address, value);
            ValidateInput(nameof(Address), value);
        }
    }

    private string? _numberOfRegisters;

    public string? NumberOfRegisters
    {
        get => _numberOfRegisters;
        set
        {
            this.RaiseAndSetIfChanged(ref _numberOfRegisters, value);
            ValidateInput(nameof(NumberOfRegisters), value);
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

    private ObservableCollection<string> _writeFunctions = new ObservableCollection<string>();

    public ObservableCollection<string> WriteFunctions
    {
        get => _writeFunctions;
        set => this.RaiseAndSetIfChanged(ref _writeFunctions, value);
    }

    private string? _selectedWriteFunction;

    public string? SelectedWriteFunction
    {
        get => _selectedWriteFunction;
        set => this.RaiseAndSetIfChanged(ref _selectedWriteFunction, value);
    }

    private IWriteField_VM? _currentWriteFieldViewModel;

    public IWriteField_VM? CurrentWriteFieldViewModel
    {
        get => _currentWriteFieldViewModel;
        set => this.RaiseAndSetIfChanged(ref _currentWriteFieldViewModel, value);
    }


    public ReactiveCommand<Unit, Unit> Command_Read { get; }
    public ReactiveCommand<Unit, Unit> Command_Write { get; }


    private NumberStyles _numberViewStyle;

    private byte _selectedSlaveID = 0;
    private ushort _selectedAddress = 0;
    private ushort _selectedNumberOfRegisters = 1;

    private readonly IWriteField_VM WriteField_MultipleCoils_VM;
    private readonly IWriteField_VM WriteField_MultipleRegisters_VM;
    private readonly IWriteField_VM WriteField_SingleCoil_VM;
    private readonly IWriteField_VM WriteField_SingleRegister_VM;

    private readonly IMessageBoxMainWindow _messageBox;
    private readonly ConnectedHost _connectedHostModel;
    private readonly Model_Settings _settingsModel;

    public RequestBuilder_VM(IMessageBoxMainWindow messageBox, ConnectedHost connectedHostModel, Model_Settings settingsModel)
    {
        _messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));
        _connectedHostModel = connectedHostModel ?? throw new ArgumentNullException(nameof(connectedHostModel));
        _settingsModel = settingsModel ?? throw new ArgumentNullException(nameof(settingsModel));

        _connectedHostModel.DeviceIsConnect += Model_DeviceIsConnect;
        _connectedHostModel.DeviceIsDisconnected += Model_DeviceIsDisconnected;

        WriteField_MultipleCoils_VM = new MultipleCoils_VM();
        WriteField_MultipleRegisters_VM = new MultipleRegisters_VM(false, _settingsModel);
        WriteField_SingleCoil_VM = new SingleCoil_VM();
        WriteField_SingleRegister_VM = new SingleRegister_VM();

        /****************************************************/
        //
        // Первоначальная настройка UI
        //
        /****************************************************/

        CheckSum_IsEnable = true;
        CheckSum_IsVisible = true;

        SelectedNumberFormat_Hex = true;

        foreach (ModbusReadFunction element in Function.AllReadFunctions)
        {
            ReadFunctions.Add(element.DisplayedName);
        }

        SelectedReadFunction = Function.ReadInputRegisters.DisplayedName;

        foreach (ModbusWriteFunction element in Function.AllWriteFunctions)
        {
            WriteFunctions.Add(element.DisplayedName);
        }

        SelectedWriteFunction = Function.PresetSingleRegister.DisplayedName;

        /****************************************************/
        //
        // Настройка свойств и команд модели отображения
        //
        /****************************************************/

        Command_Read = ReactiveCommand.Create(() =>
        {
            if (string.IsNullOrEmpty(SlaveID))
            {
                _messageBox.Show("Укажите Slave ID.", MessageType.Warning);
                return;
            }

            if (string.IsNullOrEmpty(Address))
            {
                _messageBox.Show("Укажите адрес Modbus регистра.", MessageType.Warning);
                return;
            }

            if (string.IsNullOrEmpty(NumberOfRegisters))
            {
                _messageBox.Show("Укажите количество регистров для чтения.", MessageType.Warning);
                return;
            }

            string? validationMessage = CheckReadFields();

            if (!string.IsNullOrEmpty(validationMessage))
            {
                _messageBox.Show(validationMessage, MessageType.Warning);
                return;
            }

            ModbusReadFunction ReadFunction = Function.AllReadFunctions.Single(x => x.DisplayedName == SelectedReadFunction);

            MessageBus.Current.SendMessage(
                new ModbusReadMessage(MainWindow_VM.SenderName, _selectedSlaveID, _selectedAddress, ReadFunction, _selectedNumberOfRegisters, CheckSum_IsEnable)
                );
        });
        Command_Read.ThrownExceptions.Subscribe(error => _messageBox.Show($"Возникла ошибка при попытке чтения: \n\n{error.Message}", MessageType.Error, error));

        Command_Write = ReactiveCommand.Create(() =>
        {
            if (string.IsNullOrEmpty(SlaveID))
            {
                _messageBox.Show("Укажите Slave ID.", MessageType.Warning);
                return;
            }

            if (string.IsNullOrEmpty(Address))
            {
                _messageBox.Show("Укажите адрес Modbus регистра.", MessageType.Warning);
                return;
            }

            if (CurrentWriteFieldViewModel == null)
            {
                _messageBox.Show("Не выбран тип поля записи Modbus.", MessageType.Warning);
                return;
            }

            string? validationMessage = CheckWriteFields();

            if (!string.IsNullOrEmpty(validationMessage))
            {
                _messageBox.Show(validationMessage, MessageType.Warning);
                return;
            }

            ModbusWriteFunction writeFunction = Function.AllWriteFunctions.Single(x => x.DisplayedName == SelectedWriteFunction);

            WriteData modbusWriteData = CurrentWriteFieldViewModel.GetData();

            MessageBus.Current.SendMessage(
                new ModbusWriteMessage(MainWindow_VM.SenderName, _selectedSlaveID, _selectedAddress, writeFunction, modbusWriteData.Data, modbusWriteData.NumberOfRegisters, CheckSum_IsEnable)
                );
        });
        Command_Write.ThrownExceptions.Subscribe(error => _messageBox.Show($"Возникла ошибка при попытке записи:\n\n{error.Message}", MessageType.Error, error));

        this.WhenAnyValue(x => x.SelectedNumberFormat_Hex, x => x.SelectedNumberFormat_Dec)
            .Subscribe(values =>
            {
                try
                {
                    if (values.Item1 == true && values.Item2 == true)
                    {
                        return;
                    }

                    // Выбран шестнадцатеричный формат числа в полях Адрес и Данные
                    if (values.Item1)
                    {
                        SelectNumberFormat_Hex();
                    }

                    // Выбран десятичный формат числа в полях Адрес и Данные
                    else if (values.Item2)
                    {
                        SelectNumberFormat_Dec();
                    }
                }

                catch (Exception error)
                {
                    messageBox.Show($"Ошибка смены формата.\n\n{error.Message}", MessageType.Error, error);
                }
            });

        this.WhenAnyValue(x => x.SelectedWriteFunction)
            .WhereNotNull()
            .Subscribe(x =>
            {
                if (x == Function.ForceMultipleCoils.DisplayedName)
                {
                    CurrentWriteFieldViewModel = WriteField_MultipleCoils_VM;
                }

                else if (x == Function.PresetMultipleRegisters.DisplayedName)
                {
                    CurrentWriteFieldViewModel = WriteField_MultipleRegisters_VM;
                }

                else if (x == Function.ForceSingleCoil.DisplayedName)
                {
                    CurrentWriteFieldViewModel = WriteField_SingleCoil_VM;
                }

                else if (x == Function.PresetSingleRegister.DisplayedName)
                {
                    CurrentWriteFieldViewModel = WriteField_SingleRegister_VM;
                }
            });
    }

    public void Subscribe(ModbusManualMode_VM parent)
    {
        parent.CheckSum_VisibilityChanged += Parent_CheckSum_VisibilityChanged;
    }

    private void Parent_CheckSum_VisibilityChanged(object? sender, bool e)
    {
        CheckSum_IsVisible = e;
    }

    public string GetFieldViewName(string fieldName)
    {
        switch (fieldName)
        {
            case nameof(SlaveID):
                return "Slave ID";

            case nameof(Address):
                return "Адрес";

            case nameof(NumberOfRegisters):
                return "Кол-во регистров";

            default:
                return fieldName;
        }
    }

    private string? CheckWriteFields()
    {
        StringBuilder message = new StringBuilder();

        // Проверка полей в основном контроле

        if (HasErrors)
        {
            foreach (KeyValuePair<string, ValidateMessage> element in ActualErrors)
            {
                if (element.Key == nameof(NumberOfRegisters))
                {
                    continue;
                }

                message.AppendLine($"[{GetFieldViewName(element.Key)}]\n{GetFullErrorMessage(element.Key)}\n");
            }
        }

        // Проверка полей в текущем контроле записи

        if (CurrentWriteFieldViewModel != null && CurrentWriteFieldViewModel.HasValidationErrors)
        {
            message.AppendLine(CurrentWriteFieldViewModel.ValidationMessage);
        }

        if (message.Length > 0)
        {
            message.Insert(0, "Ошибки валидации:\n\n");
            return message.ToString().TrimEnd('\r', '\n');
        }

        return null;
    }

    private string? CheckReadFields()
    {
        if (!HasErrors)
        {
            return null;
        }

        StringBuilder message = new StringBuilder();

        // Проверка полей в основном контроле

        foreach (KeyValuePair<string, ValidateMessage> element in ActualErrors)
        {
            message.AppendLine($"[{GetFieldViewName(element.Key)}]\n{GetFullErrorMessage(element.Key)}\n");
        }

        if (message.Length > 0)
        {
            message.Insert(0, "Ошибки валидации:\n\n");
            return message.ToString().TrimEnd('\r', '\n');
        }

        return null;
    }

    private void SelectNumberFormat_Hex()
    {
        NumberFormat = ModbusManualMode_VM.ViewContent_NumberStyle_hex;
        _numberViewStyle = NumberStyles.HexNumber;

        if (!string.IsNullOrWhiteSpace(SlaveID) && string.IsNullOrEmpty(GetFullErrorMessage(nameof(SlaveID))))
        {
            SlaveID = _selectedSlaveID.ToString("X");
        }

        else
        {
            _selectedSlaveID = 0;
        }

        if (!string.IsNullOrWhiteSpace(Address) && string.IsNullOrEmpty(GetFullErrorMessage(nameof(Address))))
        {
            Address = _selectedAddress.ToString("X");
        }

        else
        {
            _selectedAddress = 0;
        }

        ValidateInput(nameof(SlaveID), SlaveID);
        ValidateInput(nameof(Address), Address);

        ChangeNumberStyleInErrors(nameof(SlaveID), NumberStyles.HexNumber);
        ChangeNumberStyleInErrors(nameof(Address), NumberStyles.HexNumber);
    }

    private void SelectNumberFormat_Dec()
    {
        NumberFormat = ModbusManualMode_VM.ViewContent_NumberStyle_dec;
        _numberViewStyle = NumberStyles.Number;

        if (!string.IsNullOrWhiteSpace(SlaveID) && string.IsNullOrEmpty(GetFullErrorMessage(nameof(SlaveID))))
        {
            SlaveID = int.Parse(SlaveID, NumberStyles.HexNumber).ToString();
        }

        else
        {
            _selectedSlaveID = 0;
        }

        if (!string.IsNullOrWhiteSpace(Address) && string.IsNullOrEmpty(GetFullErrorMessage(nameof(Address))))
        {
            Address = int.Parse(Address, NumberStyles.HexNumber).ToString();
        }

        else
        {
            _selectedAddress = 0;
        }

        ValidateInput(nameof(SlaveID), SlaveID);
        ValidateInput(nameof(Address), Address);

        ChangeNumberStyleInErrors(nameof(SlaveID), NumberStyles.Number);
        ChangeNumberStyleInErrors(nameof(Address), NumberStyles.Number);
    }

    private void Model_DeviceIsConnect(object? sender, IConnection? e)
    {
        UI_IsEnable = true;
    }

    private void Model_DeviceIsDisconnected(object? sender, IConnection? e)
    {
        UI_IsEnable = false;
    }

    protected override ValidateMessage? GetErrorMessage(string fieldName, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        switch (fieldName)
        {
            case nameof(SlaveID):
                return Check_SlaveID(value);

            case nameof(Address):
                return Check_Address(value);

            case nameof(NumberOfRegisters):
                return Check_NumberOfRegisters(value);
        }

        return null;
    }

    private ValidateMessage? Check_SlaveID(string value)
    {
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

    private ValidateMessage? Check_Address(string value)
    {
        if (!StringValue.IsValidNumber(value, _numberViewStyle, out _selectedAddress))
        {
            switch (_numberViewStyle)
            {
                case NumberStyles.Number:
                    return AllErrorMessages[DecError_UInt16];

                case NumberStyles.HexNumber:
                    return AllErrorMessages[HexError_UInt16];
            }
        }

        return null;
    }

    private ValidateMessage? Check_NumberOfRegisters(string value)
    {
        if (!StringValue.IsValidNumber(value, NumberStyles.Number, out _selectedNumberOfRegisters))
        {
            return AllErrorMessages[DecError_UInt16];
        }

        return null;
    }
}
