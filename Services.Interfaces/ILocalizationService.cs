using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Services.Interfaces;

/// <summary>
/// Информация об одном загруженном языке.
/// </summary>
/// <param name="Code">Короткий код языка (ru, en, de, ...).</param>
/// <param name="Name">Название языка на самом языке ("Русский", "English", ...).</param>
/// <param name="EnglishName">Название языка на английском (для логов и диагностики).</param>
public sealed record LanguageInfo(string Code, string Name, string EnglishName);

/// <summary>
/// Сервис локализации CoreBus.
///
/// Поддерживаемые языки определяются по файлам в папке Localization/ рядом с исполняемым
/// файлом приложения. Добавление нового языка = добавление нового JSON-файла,
/// код при этом не меняется.
///
/// Реализует <see cref="INotifyPropertyChanged"/>, чтобы XAML-биндинги
/// автоматически обновлялись при смене языка без перезапуска приложения.
/// </summary>
public interface ILocalizationService : INotifyPropertyChanged
{
    /// <summary>Все загруженные языки.</summary>
    IReadOnlyList<LanguageInfo> AvailableLanguages { get; }

    /// <summary>Текущий язык.</summary>
    LanguageInfo CurrentLanguage { get; }

    /// <summary>
    /// Получить перевод по ключу. Если ключа нет — возвращает сам ключ,
    /// чтобы в UI было видно, какая строка не переведена.
    /// </summary>
    string this[string key] { get; }

    /// <summary>
    /// То же, что и индексатор, но с поддержкой <see cref="string.Format(string, object?[])"/>.
    /// Используется в ViewModels для MessageBox и форматируемых сообщений.
    /// </summary>
    string Get(string key, params object[] args);

    /// <summary>
    /// Переключить язык по коду. Если код не найден — переключение игнорируется.
    /// </summary>
    void SetLanguage(string code);

    /// <summary>
    /// Событие, которое можно использовать в не-UI коде для реакции на смену языка.
    /// </summary>
    event EventHandler? LanguageChanged;
}
