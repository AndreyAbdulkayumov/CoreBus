using Avalonia.Data.Converters;
using Services.Interfaces;
using System;
using System.Globalization;

namespace CoreBus.Base.Localization;

/// <summary>
/// Avalonia-конвертер, превращающий ключ локализации (строку вида "Common.Close")
/// в текущее локализованное значение. Используется в ItemTemplate'ах для ComboBox'ов,
/// которые биндятся к коллекциям ключей, а не к готовым строкам.
///
/// Не реактивен — перерисовывается вместе с ItemsSource/SelectedItem. Для автоматического
/// обновления при смене языка используется {l:Loc ...} или подписка на LanguageChanged в VM.
/// </summary>
public class LocalizeKeyConverter : IValueConverter
{
    public static readonly LocalizeKeyConverter Instance = new LocalizeKeyConverter();

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
        // Обратное преобразование (текст → ключ) не поддерживается — биндинг OneWay.
        throw new NotSupportedException();
    }
}
