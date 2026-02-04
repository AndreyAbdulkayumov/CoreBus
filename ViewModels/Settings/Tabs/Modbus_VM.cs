using ReactiveUI;
using System.Globalization;
using ViewModels.Helpers.FloatNumber;
using ViewModels.Validation;

namespace ViewModels.Settings.Tabs;

public class Modbus_VM : ValidatedDateInput, IValidationFieldInfo
{
    private string _writeTimeout = string.Empty;

    public string WriteTimeout
    {
        get => _writeTimeout;
        set
        {
            this.RaiseAndSetIfChanged(ref _writeTimeout, value);
            ValidateInput(nameof(WriteTimeout), value);
        }
    }

    private string _readTimeout = string.Empty;

    public string ReadTimeout
    {
        get => _readTimeout;
        set
        {
            this.RaiseAndSetIfChanged(ref _readTimeout, value);
            ValidateInput(nameof(ReadTimeout), value);
        }
    }

    private FloatNumberFormat _floatFormat;

    public FloatNumberFormat FloatFormat
    {
        get => _floatFormat;
        set => this.RaiseAndSetIfChanged(ref _floatFormat, value);
    }

    public Modbus_VM()
    {

    }

    #region Валидация

    public string GetFieldViewName(string fieldName)
    {
        switch (fieldName)
        {
            case nameof(WriteTimeout):
                return "Таймаут записи";

            case nameof(ReadTimeout):
                return "Таймаут чтения";

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

        if (!StringValue.IsValidNumber(value, NumberStyles.Number, out uint _))
        {
            return AllErrorMessages[DecError_uint];
        }

        return null;
    }

    #endregion Валидация
}
