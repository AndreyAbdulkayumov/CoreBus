using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MessageBox.AvaloniaUI.Localization;

public static class Localizer
{
    private static ILocalizationService? _instance;
    private static readonly Dictionary<string, LocValue> _values = new(StringComparer.Ordinal);
    private static readonly object _sync = new();

    public static ILocalizationService Instance
    {
        get => _instance ?? throw new InvalidOperationException(
            "MessageBox Localizer is not initialized. Set Localizer.Instance before loading MessageBox XAML.");
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

    internal static LocValue GetOrCreate(string key)
    {
        lock (_sync)
        {
            if (!_values.TryGetValue(key, out var locValue))
            {
                locValue = new LocValue(key);
                locValue.Update(_instance is null ? key : _instance[key]);
                _values[key] = locValue;
            }

            return locValue;
        }
    }

    private static void OnLanguageChanged(object? sender, EventArgs e) => RefreshAll();

    private static void RefreshAll()
    {
        if (_instance is null)
        {
            return;
        }

        lock (_sync)
        {
            foreach (var pair in _values)
            {
                pair.Value.Update(_instance[pair.Key]);
            }
        }
    }
}

public sealed class LocValue : INotifyPropertyChanged
{
    private string _value;

    internal LocValue(string key)
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

    internal void Update(string newValue) => Value = newValue ?? Key;

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class LocExtension : MarkupExtension
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
        return new Binding(nameof(LocValue.Value))
        {
            Source = locValue,
            Mode = BindingMode.OneWay,
            FallbackValue = Key
        };
    }
}
