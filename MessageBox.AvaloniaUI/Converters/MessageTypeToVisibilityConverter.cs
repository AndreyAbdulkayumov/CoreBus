using Avalonia.Data.Converters;
using MessageBox.Core;
using System;
using System.Globalization;

namespace MessageBox.AvaloniaUI.Converters;

public class MessageTypeToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MessageType messageType && parameter is string param)
        {
            var enumValue = param switch
            {
                "Information" => MessageType.Information,
                "Warning" => MessageType.Warning,
                "Error" => MessageType.Error,
                _ => throw new ArgumentException("Invalid converter parameter."),
            };

            return messageType == enumValue ? true : false;
        }

        throw new ArgumentException("Invalid converter input.");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}