using Core.Models.Settings;
using MessageBox.Core;
using ReactiveUI;
using Services.Interfaces;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reactive;
using ViewModels.Helpers.FloatNumber;
using ViewModels.Validation;

namespace ViewModels.ModbusClient.Monitoring
{
    public class MonitoringItem_VM : ValidatedDateInput, IValidationFieldInfo
    {
        public event EventHandler<EventArgs>? TypeChanged;

        public const string TypeName_UInt16 = "UInt16";
        public const string TypeName_Int16 = "Int16";
        public const string TypeName_UInt32 = "UInt32";
        public const string TypeName_Int32 = "Int32";
        public const string TypeName_Float = "Float";

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
            set => this.RaiseAndSetIfChanged(ref  _visibleOnlyRawValue, value);
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

        private ObservableCollection<string> _allValueTypes = new ObservableCollection<string>()
        {
            TypeName_UInt16, TypeName_Int16, TypeName_UInt32, TypeName_Int32, TypeName_Float
        };

        public ObservableCollection<string> AllValueTypes
        {
            get => _allValueTypes;
        }

        private string? _selectedValueType;

        public string? SelectedValueType
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

        private bool _onChart;

        public bool OnChart
        {
            get => _onChart;
            set => this.RaiseAndSetIfChanged(ref _onChart, value);
        }

        public ReactiveCommand<Unit, Unit> Command_FormulaChange { get; }

        private NumberStyles _numberViewStyle;

        private UInt16 _selectedAddress;

        public UInt16 SelectedAddress => _selectedAddress;

        public readonly Guid Id;

        private UInt16 _rawValue = 0;
        private float _convertedInnerValue = 0;

        private readonly Model_Settings _settingsModel;


        public MonitoringItem_VM(int initAddress, NumberStyles numberStyle, Model_Settings settingsModel, IMessageBoxMainWindow messageBox)
        {
            _settingsModel = settingsModel ?? throw new ArgumentNullException(nameof(settingsModel));

            Id = Guid.NewGuid();

            _numberViewStyle = numberStyle;

            _selectedAddress = (UInt16)initAddress;
            Address = GetDisplayedAddress();            

            Value = GetDisplayedRawValue();

            TypedValue = "0";
            SelectedValueType = AllValueTypes.First();
            ConvertedValue = "0.00";

            Command_FormulaChange = ReactiveCommand.Create(() =>
            {

            });
            Command_FormulaChange.ThrownExceptions.Subscribe(error => messageBox.Show($"Ошибка изменения формулы.\n\n{error.Message}", MessageType.Error, error));

            this.WhenAnyValue(e => e.Alias)
                .Subscribe(alias =>
                {
                    AliasOpacity = string.IsNullOrEmpty(alias) ? 0.3 : 1;
                });
        }

        public void SetReadedValue(UInt16 newRawValue, Dictionary<int, UInt16> registers)
        {
            IsNewValue = _rawValue != newRawValue;

            _rawValue = newRawValue;

            Value = GetDisplayedRawValue();

            // _convertedInnerValue берется из типизированного значения.
            // Затем именно _convertedInnerValue преобразовывается по формуле.

            TypedValue = GetDisplayedTypedValue(registers, out _convertedInnerValue);

            ConvertedValue = _convertedInnerValue.ToString();
        }

        public void Clear()
        {
            IsNewValue = false;

            _rawValue = 0;
            _convertedInnerValue = 0;

            Value = "0";
            TypedValue = "0";
            ConvertedValue = "0.00";
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

        private string GetDisplayedAddress()
        {
            return _numberViewStyle == NumberStyles.HexNumber ? _selectedAddress.ToString("X") : _selectedAddress.ToString();
        }

        private string GetDisplayedRawValue()
        {
            return _numberViewStyle == NumberStyles.HexNumber ? _rawValue.ToString("X") : _rawValue.ToString();
        }

        private string GetDisplayedTypedValue(Dictionary<int, UInt16> registers, out float convertedValue)
        {

            switch (SelectedValueType)
            {
                case MonitoringItem_VM.TypeName_UInt16:
                    convertedValue = _rawValue;
                    return _numberViewStyle == NumberStyles.HexNumber ? _rawValue.ToString("X") : _rawValue.ToString();

                case MonitoringItem_VM.TypeName_Int16:
                    Int32 resultInt16 = (Int16)_rawValue;
                    convertedValue = resultInt16;
                    return _numberViewStyle == NumberStyles.HexNumber ? resultInt16.ToString("X") : resultInt16.ToString();

                case MonitoringItem_VM.TypeName_UInt32:                     
                    UInt32 resultUInt32 = BitConverter.ToUInt32(GetBytesFromRegisters(registers, 2));
                    convertedValue = resultUInt32;
                    return _numberViewStyle == NumberStyles.HexNumber ? resultUInt32.ToString("X") : resultUInt32.ToString();

                case MonitoringItem_VM.TypeName_Int32:
                    Int32 resultInt32 = BitConverter.ToInt32(GetBytesFromRegisters(registers, 2));
                    convertedValue = resultInt32;
                    return _numberViewStyle == NumberStyles.HexNumber ? resultInt32.ToString("X") : resultInt32.ToString();

                case MonitoringItem_VM.TypeName_Float:
                    float resultFloat = GetFloatNumber(registers, 2);
                    convertedValue = resultFloat;
                    return resultFloat.ToString();

                default:
                    convertedValue = _rawValue;
                    return _numberViewStyle == NumberStyles.HexNumber ? _rawValue.ToString("X") : _rawValue.ToString();
            }
        }

        private byte[] GetBytesFromRegisters(Dictionary<int, UInt16> registers, int numberOfRegisters)
        {
            return registers
                .SkipWhile(kvp => kvp.Key != _selectedAddress)
                .Take(numberOfRegisters)
                .Select(e => e.Value)
                .SelectMany(BitConverter.GetBytes)
                .ToArray();
        }

        private float GetFloatNumber(Dictionary<int, UInt16> registers, int numberOfRegisters)
        {
            var bytes = GetBytesFromRegisters(registers, numberOfRegisters);
            var floatFormat = FloatHelper.GetFloatNumberFormatOrDefault(_settingsModel.Settings?.FloatNumberFormat);

            return FloatHelper.GetFloatNumberFromBytes(bytes, floatFormat);
        }

        public string GetFieldViewName(string fieldName)
        {
            switch (fieldName)
            {
                case nameof(Address):
                    return "Адрес";

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
    }
}
