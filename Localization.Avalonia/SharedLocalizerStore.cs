using System.ComponentModel;

namespace Localization.Avalonia;

/// <summary>
/// Общий store локализованных значений для XAML расширений.
/// Позволяет нескольким assembly делить один кэш переводов.
/// </summary>
public static class SharedLocalizerStore
{
    private static ILocalizationService? _instance;
    private static readonly Dictionary<string, SharedLocValue> Values = new(StringComparer.Ordinal);
    private static readonly object Sync = new();

    public static ILocalizationService Instance
    {
        get => _instance ?? throw new InvalidOperationException(
            "SharedLocalizerStore.Instance is not initialized. Set it in App startup before loading XAML.");
        set
        {
            if (_instance is not null)
            {
                _instance.LanguageChanged -= OnLanguageChanged;
            }

            _instance = value ?? throw new ArgumentNullException(nameof(value));
            _instance.LanguageChanged += OnLanguageChanged;
            RefreshAll();
        }
    }

    public static SharedLocValue GetOrCreate(string key)
    {
        lock (Sync)
        {
            if (!Values.TryGetValue(key, out var locValue))
            {
                locValue = new SharedLocValue(key);
                locValue.Update(_instance is null ? key : _instance[key]);
                Values[key] = locValue;
            }

            return locValue;
        }
    }

    private static void OnLanguageChanged(object? sender, EventArgs e)
    {
        RefreshAll();
    }

    private static void RefreshAll()
    {
        if (_instance is null)
        {
            return;
        }

        lock (Sync)
        {
            foreach (var pair in Values)
            {
                pair.Value.Update(_instance[pair.Key]);
            }
        }
    }
}

public sealed class SharedLocValue : INotifyPropertyChanged
{
    private string _value;

    internal SharedLocValue(string key)
    {
        Key = key;
        _value = key;
    }

    public string Key { get; }

    public string Value
    {
        get => _value;
        private set
        {
            if (_value == value)
            {
                return;
            }

            _value = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }

    internal void Update(string newValue)
    {
        Value = newValue ?? Key;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
