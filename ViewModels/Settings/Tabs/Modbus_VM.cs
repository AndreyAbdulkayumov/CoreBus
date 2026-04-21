using Core.Models.Settings.FileTypes;
using ReactiveUI;
using Services.Interfaces;
using System.Globalization;
using ViewModels.Helpers.FloatNumber;
using ViewModels.Validation;

namespace ViewModels.Settings.Tabs;

public record TimestampFormatItem(TimestampFormat Value, string Display);

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

    public IEnumerable<TimestampFormatItem> TimestampFormats =>
        [
            new TimestampFormatItem(TimestampFormat.None, _localization.Get("Modbus.TimestampNone")),
            new TimestampFormatItem(TimestampFormat.Time, _localization.Get("Modbus.TimestampTimeOnly")),
            new TimestampFormatItem(TimestampFormat.DateTime, _localization.Get("Modbus.TimestampDateTime")),
            new TimestampFormatItem(TimestampFormat.ISO8601, "ISO 8601")
        ];


    private TimestampFormatItem? _selectedTimestampFormat;

    public TimestampFormatItem? SelectedTimestampFormat
    {
        get => _selectedTimestampFormat;
        set => this.RaiseAndSetIfChanged(ref _selectedTimestampFormat, value);
    }

    private FloatNumberFormat _floatFormat;

    public FloatNumberFormat FloatFormat
    {
        get => _floatFormat;
        set => this.RaiseAndSetIfChanged(ref _floatFormat, value);
    }

    private readonly ILocalizationService _localization;

    public Modbus_VM(ILocalizationService localization)
    {
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));

        _localization.LanguageChanged += (_, _) =>
        {
            // Перерисовать список при смене языка и сохранить текущий выбор
            var current = SelectedTimestampFormat?.Value;
            this.RaisePropertyChanged(nameof(TimestampFormats));
            if (current.HasValue)
            {
                SelectedTimestampFormat = TimestampFormats.FirstOrDefault(e => e.Value == current.Value);
            }
        };
    }

    public void PresetLogTimestampFormat(TimestampFormat format)
    {
        SelectedTimestampFormat = TimestampFormats.First(e => e.Value == format);
    }

    public TimestampFormat GetLogTimestampFormat()
    {
        return SelectedTimestampFormat?.Value ?? DeviceData.LogTimestampFormat_Default;
    }

    #region Валидация

    public string GetFieldViewName(string fieldName)
    {
        switch (fieldName)
        {
            case nameof(WriteTimeout):
                return _localization.Get("Common.WriteTimeoutField");

            case nameof(ReadTimeout):
                return _localization.Get("Common.ReadTimeoutField");

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
