using ReactiveUI;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Core.Models.Modbus.DataTypes;
using Core.Models.Settings.DataTypes;
using Core.Models.Settings.FileTypes;
using MessageBox.Core;
using ViewModels.Macros.DataTypes;
using ViewModels.ModbusClient;
using ViewModels.ModbusClient.WriteFields;
using ViewModels.ModbusClient.WriteFields.DataTypes;
using ViewModels.Validation;
using Core.Models.Settings;

namespace ViewModels.Macros.MacrosEdit.CommandEdit;

public class ModbusCommand_VM : ValidatedDateInput, IValidationFieldInfo, ICommandContent, ICommandValidation
{
    private readonly Guid _id;

    public Guid Id => _id;

    private string? _name = string.Empty;

    public string? Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    private bool _useCommonSlaveId;

    public bool UseCommonSlaveId
    {
        get => _useCommonSlaveId;
        set => this.RaiseAndSetIfChanged(ref _useCommonSlaveId, value);
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

    private bool _selectedFunctionType_Read;

    public bool SelectedFunctionType_Read
    {
        get => _selectedFunctionType_Read;
        set => this.RaiseAndSetIfChanged(ref _selectedFunctionType_Read, value);
    }

    private bool _selectedFunctionType_Write;

    public bool SelectedFunctionType_Write
    {
        get => _selectedFunctionType_Write;
        set => this.RaiseAndSetIfChanged(ref _selectedFunctionType_Write, value);
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

    private string? _numberOfReadRegisters;

    public string? NumberOfReadRegisters
    {
        get => _numberOfReadRegisters;
        set
        {
            this.RaiseAndSetIfChanged(ref _numberOfReadRegisters, value);
            ValidateInput(nameof(NumberOfReadRegisters), value);
        }
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

    private NumberStyles _numberViewStyle;

    private byte _selectedSlaveID = 0;
    private ushort _selectedAddress = 0;
    private ushort _selectedNumberOfReadRegisters = 1;

    private readonly IWriteField_VM WriteField_MultipleCoils_VM;
    private readonly IWriteField_VM WriteField_MultipleRegisters_VM;
    private readonly IWriteField_VM WriteField_SingleCoil_VM;
    private readonly IWriteField_VM WriteField_SingleRegister_VM;

    public ModbusCommand_VM(Guid id, EditCommandParameters parameters, IMessageBox messageBox, Model_Settings settingsModel)
    {
        _id = id;

        WriteField_MultipleCoils_VM = new MultipleCoils_VM();
        WriteField_MultipleRegisters_VM = new MultipleRegisters_VM(true, settingsModel);
        WriteField_SingleCoil_VM = new SingleCoil_VM();
        WriteField_SingleRegister_VM = new SingleRegister_VM();

        InitUI(parameters);

        /****************************************************/
        //
        // Настройка свойств и команд модели отображения
        //
        /****************************************************/

        this.WhenAnyValue(x => x.SelectedWriteFunction)
            .WhereNotNull()
            .Subscribe(SetWriteFieldVM);

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
    }

    private void InitUI(EditCommandParameters parameters)
    {
        SelectedNumberFormat_Hex = true;

        foreach (ModbusReadFunction element in Function.AllReadFunctions)
        {
            ReadFunctions.Add(element.DisplayedName);
        }

        foreach (ModbusWriteFunction element in Function.AllWriteFunctions)
        {
            WriteFunctions.Add(element.DisplayedName);
        }

        Name = parameters.CommandName;

        if (parameters.InitData is MacrosCommandModbus data && data.Content != null)
        {
            // По умолчанию формат числа hex

            SlaveID = data.Content.SlaveID.ToString();
            CheckSum_IsEnable = data.Content.CheckSum_IsEnable;
            Address = data.Content.Address.ToString();

            NumberOfReadRegisters = data.Content.NumberOfReadRegisters.ToString();

            var selectedFunction = Function.AllFunctions.First(func => func.Number == data.Content.FunctionNumber);

            if (selectedFunction is ModbusReadFunction)
            {
                SelectedFunctionType_Read = true;

                SelectedReadFunction = selectedFunction.DisplayedName;
                SelectedWriteFunction = Function.PresetSingleRegister.DisplayedName;
            }

            else
            {
                SelectedFunctionType_Write = true;

                SelectedReadFunction = Function.ReadInputRegisters.DisplayedName;
                SelectedWriteFunction = selectedFunction.DisplayedName;

                SetWriteFieldVM(selectedFunction.DisplayedName);

                if (data.Content.WriteInfo != null)
                {
                    CurrentWriteFieldViewModel?.SetDataFromMacros(data.Content.WriteInfo);
                }
            }

            return;
        }

        CheckSum_IsEnable = true;

        SelectedFunctionType_Read = true;

        SelectedReadFunction = Function.ReadInputRegisters.DisplayedName;
        SelectedWriteFunction = Function.PresetSingleRegister.DisplayedName;
    }

    private void SetWriteFieldVM(string displayedName)
    {
        if (displayedName == Function.ForceMultipleCoils.DisplayedName)
        {
            CurrentWriteFieldViewModel = WriteField_MultipleCoils_VM;
            return;
        }

        if (displayedName == Function.PresetMultipleRegisters.DisplayedName)
        {
            CurrentWriteFieldViewModel = WriteField_MultipleRegisters_VM;
            return;
        }

        if (displayedName == Function.ForceSingleCoil.DisplayedName)
        {
            CurrentWriteFieldViewModel = WriteField_SingleCoil_VM;
            return;
        }

        if (displayedName == Function.PresetSingleRegister.DisplayedName)
        {
            CurrentWriteFieldViewModel = WriteField_SingleRegister_VM;
            return;
        }

        throw new Exception($"Выбрана неизвестная функция записи \"{displayedName}\"");
    }

    public object GetContent()
    {
        var selectedFunction = SelectedFunctionType_Read ? SelectedReadFunction : SelectedWriteFunction;

        int functionNumber = Function.AllFunctions.Single(x => x.DisplayedName == selectedFunction).Number;

        ModbusMacrosWriteInfo? writeData = CurrentWriteFieldViewModel?.GetMacrosData();

        return new MacrosCommandModbus()
        {
            Name = Name,
            Content = new ModbusCommandContent()
            {
                SlaveID = _selectedSlaveID,
                Address = _selectedAddress,
                FunctionNumber = functionNumber,
                WriteInfo = SelectedFunctionType_Read ? null : writeData,
                NumberOfReadRegisters = _selectedNumberOfReadRegisters,
                CheckSum_IsEnable = CheckSum_IsEnable,
            }
        };
    }

    public string? GetValidationMessage(params FieldNames[] uncheckedFields)
    {
        List<string?> validationMessages = new List<string?>();

        if (!uncheckedFields.Contains(FieldNames.SlaveID) && string.IsNullOrWhiteSpace(SlaveID))
        {
            validationMessages.Add("Не задан SlaveID.");
        }

        if (string.IsNullOrWhiteSpace(Address))
        {
            validationMessages.Add("Не задан Адрес.");
        }

        string? writeReadMessages = SelectedFunctionType_Write ? CheckWriteFields(uncheckedFields) : CheckReadFields(uncheckedFields);

        if (!string.IsNullOrEmpty(writeReadMessages))
        {
            validationMessages.Add(writeReadMessages);
        }

        if (validationMessages.Any())
        {
            return string.Join("\n\n", validationMessages);
        }

        return null;
    }

    private string? CheckWriteFields(params FieldNames[] uncheckedFields)
    {
        StringBuilder message = new StringBuilder();

        // Проверка полей в основном контроле

        if (HasErrors)
        {
            foreach (KeyValuePair<string, ValidateMessage> element in ActualErrors)
            {
                if (element.Key == nameof(NumberOfReadRegisters))
                    continue;

                if (uncheckedFields.Contains(FieldNames.SlaveID) && element.Key == nameof(SlaveID))
                    continue;

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

    private string? CheckReadFields(params FieldNames[] uncheckedFields)
    {
        if (string.IsNullOrWhiteSpace(NumberOfReadRegisters))
        {
            return "Укажите количество регистров для чтения.";
        }

        if (_selectedNumberOfReadRegisters < 1)
        {
            return "Сколько, сколько регистров вы хотите прочитать? :)";
        }

        if (!HasErrors)
        {
            return null;
        }

        StringBuilder message = new StringBuilder();

        // Проверка полей в основном контроле

        foreach (KeyValuePair<string, ValidateMessage> element in ActualErrors)
        {
            if (uncheckedFields.Contains(FieldNames.SlaveID) && element.Key == nameof(SlaveID))
                continue;

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
        NumberFormat = ModbusClient_VM.ViewContent_NumberStyle_hex;
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
        NumberFormat = ModbusClient_VM.ViewContent_NumberStyle_dec;
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

    public string GetFieldViewName(string fieldName)
    {
        switch (fieldName)
        {
            case nameof(SlaveID):
                return "Slave ID";

            case nameof(Address):
                return "Адрес";

            case nameof(NumberOfReadRegisters):
                return "Кол-во регистров";

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
            case nameof(SlaveID):
                return Check_SlaveID(value);

            case nameof(Address):
                return Check_Address(value);

            case nameof(NumberOfReadRegisters):
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
        if (!StringValue.IsValidNumber(value, NumberStyles.Number, out _selectedNumberOfReadRegisters))
        {
            return AllErrorMessages[DecError_UInt16];
        }

        return null;
    }
}
