using MessageBox.Core;
using ReactiveUI;
using Services.Interfaces;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Reactive;
using ViewModels.Validation;

namespace ViewModels.ModbusClient.Monitoring
{
    public class MonitoringItem_VM : ValidatedDateInput, IValidationFieldInfo
    {
        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }

        private string _address;

        public string Address
        {
            get => _address;
            set 
            {
                this.RaiseAndSetIfChanged(ref _address, value);
                ValidateInput(nameof(Address), value);
            }
        }

        private string _alias;

        public string Alias
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

        private string _value;

        public string Value
        {
            get => _value;
            set => this.RaiseAndSetIfChanged(ref _value, value);
        }

        private string _typedValue;

        public string TypedValue
        {
            get => _typedValue;
            set => this.RaiseAndSetIfChanged(ref _typedValue, value);
        }

        private ObservableCollection<string> _allValueTypes = new ObservableCollection<string>()
        {
            "UInt16", "Int16", "UInt32", "Int32", "Float"
        };

        public ObservableCollection<string> AllValueTypes
        {
            get => _allValueTypes;
        }

        private string _selectedValueType;

        public string SelectedValueType
        {
            get => _selectedValueType;
            set => this.RaiseAndSetIfChanged(ref _selectedValueType, value);
        }

        private string _convertedValue;

        public string ConvertedValue
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


        public MonitoringItem_VM(int initAddress, IMessageBoxMainWindow messageBox)
        {
            Address = initAddress.ToString();
            Value = "0";
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

            if (!StringValue.IsValidNumber(value, NumberStyles.Number, out UInt16 _selectedAddress))
            {
                return AllErrorMessages[DecError_UInt16];

                // TODO: на будущее
                //switch (_numberViewStyle)
                //{
                //    case NumberStyles.Number:
                //        return AllErrorMessages[DecError_UInt16];

                //    case NumberStyles.HexNumber:
                //        return AllErrorMessages[HexError_UInt16];
                //}
            }

            return null;
        }
    }
}
