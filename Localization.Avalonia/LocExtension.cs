using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace Localization.Avalonia;

public class LocExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public LocExtension()
    {
    }

    public LocExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var locValue = Localizer.GetOrCreate(Key);
        return new Binding(nameof(SharedLocValue.Value))
        {
            Source = locValue,
            Mode = BindingMode.OneWay,
            FallbackValue = Key
        };
    }
}
