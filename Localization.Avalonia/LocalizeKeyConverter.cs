using Avalonia.Data.Converters;
using System.Globalization;

namespace Localization.Avalonia;

public class LocalizeKeyConverter : IValueConverter
{
    public static readonly LocalizeKeyConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string key && !string.IsNullOrEmpty(key))
        {
            return LocalizationProvider.Get(key);
        }

        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
