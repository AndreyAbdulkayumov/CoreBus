using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Services.Interfaces;

namespace CoreBus.Base.Localization;

/// <summary>
/// Глобальная точка доступа к <see cref="ILocalizationService"/> из XAML.
///
/// Заполняется один раз при старте приложения (в App.axaml.cs), после того
/// как построен DI-контейнер. Нужна потому, что XAML markup-extension
/// выполняется без доступа к IServiceProvider приложения.
/// </summary>
public static class Localizer
{
    private static ILocalizationService? _instance;

    // Кэш «нотификаторов» по ключам.
    // Каждому уникальному ключу соответствует один экземпляр LocValue,
    // общий для всех элементов UI, привязанных к этому ключу.
    // Это гарантирует, что при смене языка все элементы обновляются
    // через один и тот же источник.
    private static readonly Dictionary<string, LocValue> _values
        = new(StringComparer.Ordinal);

    private static readonly object _sync = new();

    /// <summary>
    /// Текущий экземпляр сервиса локализации. Устанавливается один раз при старте.
    /// </summary>
    public static ILocalizationService Instance
    {
        get => _instance ?? throw new InvalidOperationException(
            "Localizer.Instance is not initialized. Set it in App startup before loading XAML.");
        set
        {
            if (_instance is not null)
            {
                _instance.LanguageChanged -= OnLanguageChanged;
            }
            _instance = value ?? throw new ArgumentNullException(nameof(value));
            _instance.LanguageChanged += OnLanguageChanged;

            // Первично наполняем все уже созданные LocValue текущими значениями.
            RefreshAll();
        }
    }

    internal static LocValue GetOrCreate(string key)
    {
        lock (_sync)
        {
            if (!_values.TryGetValue(key, out var lv))
            {
                lv = new LocValue(key);
                lv.Update(_instance is null ? key : _instance[key]);
                _values[key] = lv;
            }
            return lv;
        }
    }

    private static void OnLanguageChanged(object? sender, EventArgs e) => RefreshAll();

    private static void RefreshAll()
    {
        if (_instance is null) return;
        lock (_sync)
        {
            foreach (var kv in _values)
            {
                kv.Value.Update(_instance[kv.Key]);
            }
        }
    }
}

/// <summary>
/// Наблюдаемое значение перевода по одному ключу.
/// Реализует <see cref="INotifyPropertyChanged"/> на конкретное свойство
/// <see cref="Value"/>, поэтому Avalonia-биндинги по пути "Value"
/// обновляются мгновенно и одновременно у всех контролов.
/// </summary>
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
            if (_value == value) return;
            _value = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }

    internal void Update(string newValue) => Value = newValue ?? Key;

    public event PropertyChangedEventHandler? PropertyChanged;

    // Для удобства — возможность использовать объект напрямую как строку
    public override string ToString() => _value;
}

/// <summary>
/// XAML-расширение разметки, подставляющее в атрибут значение локализованной
/// строки. Пример использования в axaml:
///
///   xmlns:l="clr-namespace:CoreBus.Base.Localization;assembly=CoreBus.Base"
///   ...
///   &lt;TextBlock Text="{l:Loc Common.Close}"/&gt;
///
/// Возвращает Binding к общему <see cref="LocValue"/> для указанного ключа.
/// Благодаря тому, что LocValue реализует INotifyPropertyChanged с конкретным
/// именем свойства "Value", все биндинги обновляются синхронно при смене языка.
/// </summary>
public sealed class LocExtension : MarkupExtension
{
    /// <summary>Ключ перевода, например "Common.Close".</summary>
    public string Key { get; set; } = string.Empty;

    public LocExtension() { }

    public LocExtension(string key) => Key = key;

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
