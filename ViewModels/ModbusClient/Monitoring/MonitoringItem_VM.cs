using Core.Models;
using Core.Models.Settings;
using Core.Models.Settings.FileTypes;
using DynamicData;
using MessageBox.Core;
using MessageBusTypes.Chart;
using ReactiveUI;
using Services.Interfaces;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Reactive;
using ViewModels.Helpers.FloatNumber;
using ViewModels.Validation;

namespace ViewModels.ModbusClient.Monitoring;

public record ValueTypeItem(MonitoringValueType Value, string Display);

public class MonitoringItem_VM : ValidatedDateInput, IValidationFieldInfo
{
    public bool ItemShowOnChartAndLog => ShowOnChartAndLog && !VisibleOnlyRawValue;

    public event EventHandler<EventArgs>? TypeChanged;

    private bool ui_IsEnable = true;

    public bool UI_IsEnable
    {
        get => ui_IsEnable;
        set => this.RaiseAndSetIfChanged(ref ui_IsEnable, value);
    }

    private bool _visibleOnlyRawValue;

    public bool VisibleOnlyRawValue
    {
        get => _visibleOnlyRawValue;
        set => this.RaiseAndSetIfChanged(ref _visibleOnlyRawValue, value);
    }

    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
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

    private string? _alias;

    public string? Alias
    {
        get => _alias;
        set => this.RaiseAndSetIfChanged(ref _alias, value);
    }

    private double _aliasOpacity;

    public double AliasOpacity
    {
        get => _aliasOpacity;
        set => this.RaiseAndSetIfChanged(ref _aliasOpacity, value);
    }

    private string? _value;

    public string? Value
    {
        get => _value;
        set => this.RaiseAndSetIfChanged(ref _value, value);
    }

    private bool _isNewValue;

    public bool IsNewValue
    {
        get => _isNewValue;
        set => this.RaiseAndSetIfChanged(ref _isNewValue, value);
    }

    private string? _typedValue;

    public string? TypedValue
    {
        get => _typedValue;
        set => this.RaiseAndSetIfChanged(ref _typedValue, value);
    }

    private bool _isNewTypedValue;

    public bool IsNewTypedValue
    {
        get => _isNewTypedValue;
        set => this.RaiseAndSetIfChanged(ref _isNewTypedValue, value);
    }

    private ObservableCollection<ValueTypeItem> _allValueTypes = new ObservableCollection<ValueTypeItem>();

    public IEnumerable<ValueTypeItem> _allValueTypes_Regular =>
    [
        new ValueTypeItem(MonitoringValueType.UInt16, "UInt16"),
        new ValueTypeItem(MonitoringValueType.Int16, "Int16"),
        new ValueTypeItem(MonitoringValueType.UInt32, "UInt32"),
        new ValueTypeItem(MonitoringValueType.Int32, "Int32"),
        new ValueTypeItem(MonitoringValueType.Float, "Float")
    ];

    public IEnumerable<ValueTypeItem> _allValueTypes_Last =>
    [
        new ValueTypeItem(MonitoringValueType.UInt16, "UInt16"),
        new ValueTypeItem(MonitoringValueType.Int16, "Int16")
    ];

    public ObservableCollection<ValueTypeItem> AllValueTypes
    {
        get => _allValueTypes;
        set => this.RaiseAndSetIfChanged(ref _allValueTypes, value);
    }

    private ValueTypeItem? _selectedValueType;

    public ValueTypeItem? SelectedValueType
    {
        get => _selectedValueType;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedValueType, value);
            TypeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private string? _convertedValue;

    public string? ConvertedValue
    {
        get => _convertedValue;
        set => this.RaiseAndSetIfChanged(ref _convertedValue, value);
    }

    /// <summary>
    /// Полное преобразованное значение для записи в лог и отображения на графике.
    /// </summary>
    public double FullConvertedValue { get; private set; }

    private bool _isNewConvertedValue;

    public bool IsNewConvertedValue
    {
        get => _isNewConvertedValue;
        set => this.RaiseAndSetIfChanged(ref _isNewConvertedValue, value);
    }

    private string? _formula;

    public string? Formula
    {
        get => _formula;
        set => this.RaiseAndSetIfChanged(ref _formula, value);
    }

    private bool _showOnChartAndLog;

    public bool ShowOnChartAndLog
    {
        get => _showOnChartAndLog;
        set => this.RaiseAndSetIfChanged(ref _showOnChartAndLog, value);
    }

    public ReactiveCommand<Unit, Unit> Command_FormulaChange { get; }

    private UInt16 _selectedAddress;

    public UInt16 SelectedAddress => _selectedAddress;

    private const string DefaultFormula = "x";

    private const int _floatRoundedDigit = 2;

    private NumberStyles _numberViewStyle;

    public readonly Guid Id;

    private UInt16 _rawValue;
    private float _typedInnerValue;

    private readonly Model_Settings _settingsModel;
    private readonly IOpenChildWindowService _openChildWindowService;
    private readonly IMessageBoxMainWindow _messageBox;

    public MonitoringItem_VM(int initAddress, NumberStyles numberStyle, Model_Settings settingsModel, IOpenChildWindowService openChildWindowService, IMessageBoxMainWindow messageBox)
    {
        _settingsModel = settingsModel ?? throw new ArgumentNullException(nameof(settingsModel));
        _openChildWindowService = openChildWindowService ?? throw new ArgumentNullException(nameof(openChildWindowService));
        _messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));

        /****************************************************/
        //
        // Первоначальная инициализация компонента
        //
        /****************************************************/

        Id = Guid.NewGuid();

        _numberViewStyle = numberStyle;

        _selectedAddress = (UInt16)initAddress;
        Address = GetDisplayedAddress();

        SelectedValueType = _allValueTypes_Last.First();
        Formula = DefaultFormula;

        /****************************************************/
        //
        // Настройка свойств и команд модели отображения
        //
        /****************************************************/

        Command_FormulaChange = ReactiveCommand.CreateFromTask(async () =>
        {
            var description = string.IsNullOrWhiteSpace(Alias) ? LocalizationProvider.Get("Monitoring.ForAddress", GetDisplayedAddress()) : Alias;

            var newFormula = await _openChildWindowService.EditFormula(description, Formula, UI_IsEnable);

            if (!string.IsNullOrEmpty(newFormula))
            {
                Formula = newFormula;
            }
        });
        Command_FormulaChange.ThrownExceptions.Subscribe(error => _messageBox.Show(LocalizationProvider.Get("Error.FormulaChange") + "\n\n" + error.Message, MessageType.Error, error));

        this.WhenAnyValue(e => e.Alias)
            .Subscribe(alias =>
            {
                AliasOpacity = string.IsNullOrEmpty(alias) ? 0.3 : 1;
            });

        // Действия после создания

        SetDefaultValues();
    }

    public string GetDisplayedItemName()
    {
        return Alias ?? LocalizationProvider.Get("Monitoring.AddressFallback", Address);
    }

    private void SetDefaultValues()
    {
        IsNewValue = false;
        IsNewTypedValue = false;
        IsNewConvertedValue = false;

        _rawValue = 0;
        _typedInnerValue = 0;

        Value = "0";
        TypedValue = "0";
        ConvertedValue = "0";
    }

    public void SetExistingValues(ModbusMonitoringItemData initData)
    {
        Alias = initData.Alias;
        SelectedValueType = _allValueTypes_Regular.First(e => e.Value == initData.ValueType);
        VisibleOnlyRawValue = initData.VisibleOnlyRawValue;
        Formula = string.IsNullOrWhiteSpace(initData.Formula) ? DefaultFormula : initData.Formula;
        ShowOnChartAndLog = initData.ShowOnChartAndLog;
    }

    public void SetReadedValue(UInt16 newRawValue, IReadOnlyList<(int address, UInt16 value)> registers, uint chartIncrementX)
    {
        IsNewValue = _rawValue != newRawValue;

        _rawValue = newRawValue;

        Value = GetDisplayedRawValue();

        // _typedInnerValue это внутреннее типизированное значение, представленное как float.
        // Затем именно _typedInnerValue преобразовывается по формуле.

        var oldTypedValue = TypedValue;

        TypedValue = GetDisplayedTypedValue(registers, out _typedInnerValue);

        IsNewTypedValue = TypedValue != oldTypedValue;

        if (string.IsNullOrEmpty(Formula))
            return;

        var oldConvertedValue = ConvertedValue;

        ConvertedValue = GetDisplayedConvertedValue(Formula, _typedInnerValue, out double convertResult);

        FullConvertedValue = convertResult;

        IsNewConvertedValue = ConvertedValue != oldConvertedValue;

        if (ShowOnChartAndLog && _openChildWindowService.ChartWindowIsOpen)
        {
            MessageBus.Current.SendMessage(
                new AddingPointMessage(Id, FullConvertedValue)
                );
        }
    }

    public void Clear()
    {
        SetDefaultValues();
    }

    public void SetNumberFormat(NumberStyles newStyle)
    {
        _numberViewStyle = newStyle;

        if (!string.IsNullOrWhiteSpace(Address) && string.IsNullOrEmpty(GetFullErrorMessage(nameof(Address))))
        {
            Address = GetDisplayedAddress();
        }

        else
        {
            _selectedAddress = 0;
        }

        Value = GetDisplayedRawValue();

        ValidateInput(nameof(Address), Address);

        ChangeNumberStyleInErrors(nameof(Address), newStyle);
    }

    public void SetAsLast(bool isLast)
    {
        var selectedType = SelectedValueType;

        AllValueTypes.Clear();
        AllValueTypes.AddRange(isLast ? _allValueTypes_Last : _allValueTypes_Regular);

        if (selectedType != null && AllValueTypes.Contains(selectedType))
        {
            SelectedValueType = selectedType;
            return;
        }

        SelectedValueType = AllValueTypes.First();
    }

    private string GetDisplayedAddress()
    {
        return _numberViewStyle == NumberStyles.HexNumber ? _selectedAddress.ToString("X") : _selectedAddress.ToString();
    }

    private string GetDisplayedRawValue()
    {
        return _numberViewStyle == NumberStyles.HexNumber ? _rawValue.ToString("X") : _rawValue.ToString();
    }

    private string GetDisplayedTypedValue(IReadOnlyList<(int address, UInt16 value)> registers, out float typedValue)
    {
        switch (SelectedValueType?.Value)
        {
            case MonitoringValueType.UInt16:
                typedValue = _rawValue;
                return _rawValue.ToString();

            case MonitoringValueType.Int16:
                Int32 resultInt16 = (Int16)_rawValue;
                typedValue = resultInt16;
                return resultInt16.ToString();

            case MonitoringValueType.UInt32:
                UInt32 resultUInt32 = BitConverter.ToUInt32(GetBytesFromRegisters(registers, 2));
                typedValue = resultUInt32;
                return resultUInt32.ToString();

            case MonitoringValueType.Int32:
                Int32 resultInt32 = BitConverter.ToInt32(GetBytesFromRegisters(registers, 2));
                typedValue = resultInt32;
                return resultInt32.ToString();

            case MonitoringValueType.Float:
                float resultFloat = GetFloatNumber(registers);
                typedValue = resultFloat;
                return resultFloat == 0f ? "0" : resultFloat.ToString();    // Исключаем конвертацию в -0 у чисел типа float

            default:
                typedValue = _rawValue;
                return _rawValue.ToString();
        }
    }

    private string GetDisplayedConvertedValue(string formula, float typedValue, out double convertResult)
    {
        convertResult = MathFormula.Solve(formula, typedValue);

        var roundedValue = Math.Round(convertResult, _floatRoundedDigit);

        return roundedValue == 0f ? "0" : roundedValue.ToString();    // Исключаем конвертацию в -0 у чисел типа double
    }

    private byte[] GetBytesFromRegisters(IReadOnlyList<(int address, UInt16 value)> registers, int numberOfRegisters)
    {
        return registers
            .SkipWhile(e => e.address != _selectedAddress)
            .Take(numberOfRegisters)
            .Select(e => e.value)
            .SelectMany(BitConverter.GetBytes)
            .ToArray();
    }

    private float GetFloatNumber(IReadOnlyList<(int address, UInt16 value)> registers)
    {
        var bytes = GetBytesFromRegisters(registers, 2);    // т.к. для float числа используется 2 регистра
        var floatFormat = FloatHelper.GetFloatNumberFormatOrDefault(_settingsModel.Settings?.FloatNumberFormat);

        return MathF.Round(FloatHelper.GetFloatNumberFromBytes(bytes, floatFormat), _floatRoundedDigit);
    }

    #region Валидация

    public string GetFieldViewName(string fieldName)
    {
        switch (fieldName)
        {
            case nameof(Address):
                return LocalizationProvider.Get("Common.Address");

            default:
                return fieldName;
        }
    }

    protected override ValidateMessage? GetErrorMessage(string fieldName, string? value)
    {
        switch (fieldName)
        {
            case nameof(Address):
                return Check_Address(value);
        }

        return null;
    }

    private ValidateMessage? Check_Address(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return AllErrorMessages[NotEmptyField];
        }

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

    #endregion Валидация
}
