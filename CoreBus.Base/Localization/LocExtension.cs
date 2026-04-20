using System;
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
    /// <summary>
    /// Текущий экземпляр сервиса локализации. Устанавливается один раз при старте.
    /// </summary>
    public static ILocalizationService Instance { get; set; } = null!;
}

/// <summary>
/// XAML-расширение разметки, подставляющее в атрибут значение локализованной
/// строки. Пример использования в axaml:
///
///   xmlns:l="clr-namespace:CoreBus.Base.Localization;assembly=CoreBus.Base"
///   ...
///   &lt;TextBlock Text="{l:Loc Common.Close}"/&gt;
///
/// Привязывается к индексатору <see cref="ILocalizationService"/> через Binding,
/// благодаря чему при смене языка все элементы UI обновляются автоматически
/// — не требуется перезапуск и не требуется какой-либо дополнительный код.
/// </summary>
public sealed class LocExtension : MarkupExtension
{
    /// <summary>Ключ перевода, например "Common.Close".</summary>
    public string Key { get; set; } = string.Empty;

    public LocExtension() { }

    public LocExtension(string key) => Key = key;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        // Возвращаем Binding с путём-индексатором [Key].
        // Источник — глобальный экземпляр сервиса (реализует INotifyPropertyChanged),
        // так что при смене языка биндинги сами перечитают значения.
        return new Binding($"[{Key}]")
        {
            Source = Localizer.Instance,
            Mode = BindingMode.OneWay,
            Priority = BindingPriority.LocalValue,
            FallbackValue = Key
        };
    }
}
