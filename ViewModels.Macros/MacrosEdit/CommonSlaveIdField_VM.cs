using Core.Models.Settings.FileTypes;
using MessageBox.Core;
using ReactiveUI;
using Services.Interfaces;
using System.Globalization;
using System.Text;
using ViewModels.Validation;

namespace ViewModels.Macros.MacrosEdit
{
    public class CommonSlaveIdField_VM : ValidatedDateInput, IValidationFieldInfo
    {
        public event EventHandler<bool>? UseCommonSlaveIdChanged;

        private bool _useCommonSlaveId;

        public bool UseCommonSlaveId
        {
            get => _useCommonSlaveId;
            set => this.RaiseAndSetIfChanged(ref _useCommonSlaveId, value);
        }

        private string? _commonSlaveId;

        public string? CommonSlaveId
        {
            get => _commonSlaveId;
            set
            {
                this.RaiseAndSetIfChanged(ref _commonSlaveId, value);
                ValidateInput(nameof(CommonSlaveId), value);
            }
        }

        private bool _numberFormat_Hex;

        public bool NumberFormat_Hex
        {
            get => _numberFormat_Hex;
            set => this.RaiseAndSetIfChanged(ref _numberFormat_Hex, value);
        }

        private bool _numberFormat_Dec;

        public bool NumberFormat_Dec
        {
            get => _numberFormat_Dec;
            set => this.RaiseAndSetIfChanged(ref _numberFormat_Dec, value);
        }

        private NumberStyles _numberViewStyle;
        private byte _selectedSlaveId;

        public CommonSlaveIdField_VM(IMessageBox messageBox)
        {
            NumberFormat_Hex = true;

            this.WhenAnyValue(x => x.UseCommonSlaveId)
                .Subscribe(x => UseCommonSlaveIdChanged?.Invoke(this, x));

            this.WhenAnyValue(x => x.NumberFormat_Hex, x => x.NumberFormat_Dec)
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
                        messageBox.Show(LocalizationProvider.Get("Error.FormatChange") + "\n\n" + error.Message, MessageType.Error, error);
                    }
                });
        }

        public void SetSlaveId(byte value)
        {
            _selectedSlaveId = value;

            CommonSlaveId = NumberFormat_Hex ? _selectedSlaveId.ToString("X") : _selectedSlaveId.ToString();
        }

        public ModbusAdditionalData GetAdditionalData()
        {
            return new ModbusAdditionalData()
            {
                UseCommonSlaveId = UseCommonSlaveId,
                CommonSlaveId = _selectedSlaveId,
            };
        }

        public string? GetErrorMessage()
        {
            if (!UseCommonSlaveId) 
                return null;

            string errorSource = LocalizationProvider.Get("Macros.AdditionalSettingsErrors") + "\n\n";

            if (string.IsNullOrWhiteSpace(CommonSlaveId))
            {
                return errorSource + LocalizationProvider.Get("Macros.CommonSlaveIdNotSet");
            }

            StringBuilder message = new StringBuilder();

            if (!HasErrors) 
                return null;

            foreach (KeyValuePair<string, ValidateMessage> element in ActualErrors)
            {
                message.AppendLine($"[{GetFieldViewName(element.Key)}]\n{GetFullErrorMessage(element.Key)}\n");
            }

            if (message.Length > 0)
            {
                message.Insert(0, errorSource + LocalizationProvider.Get("Validation.ErrorsHeader") + "\n\n");
                return message.ToString().TrimEnd('\r', '\n');
            }

            return null;
        }

        private void SelectNumberFormat_Hex()
        {
            _numberViewStyle = NumberStyles.HexNumber;

            if (!string.IsNullOrWhiteSpace(CommonSlaveId) && string.IsNullOrEmpty(GetFullErrorMessage(nameof(CommonSlaveId))))
            {
                CommonSlaveId = _selectedSlaveId.ToString("X");
            }

            else
            {
                _selectedSlaveId = 0;
            }

            ValidateInput(nameof(CommonSlaveId), CommonSlaveId);

            ChangeNumberStyleInErrors(nameof(CommonSlaveId), NumberStyles.HexNumber);
        }

        private void SelectNumberFormat_Dec()
        {
            _numberViewStyle = NumberStyles.Number;

            if (!string.IsNullOrWhiteSpace(CommonSlaveId) && string.IsNullOrEmpty(GetFullErrorMessage(nameof(CommonSlaveId))))
            {
                CommonSlaveId = int.Parse(CommonSlaveId, NumberStyles.HexNumber).ToString();
            }

            else
            {
                _selectedSlaveId = 0;
            }

            ValidateInput(nameof(CommonSlaveId), CommonSlaveId);

            ChangeNumberStyleInErrors(nameof(CommonSlaveId), NumberStyles.Number);
        }

        # region Валидация

        public string GetFieldViewName(string fieldName)
        {
            switch (fieldName)
            {
                case nameof(CommonSlaveId):
                    return "Slave ID";

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
                case nameof(CommonSlaveId):
                    return Check_SlaveID(value);
            }

            return null;
        }

        private ValidateMessage? Check_SlaveID(string value)
        {
            if (!StringValue.IsValidNumber(value, _numberViewStyle, out _selectedSlaveId))
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

        #endregion Валидация
    }
}
