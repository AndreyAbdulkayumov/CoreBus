using System.ComponentModel;

namespace Localization.Interfaces;

/// <summary>
/// Информация об одном загруженном языке.
/// </summary>
/// <param name="Code">Короткий код языка (ru, en, de, ...).</param>
/// <param name="Name">Название языка на самом языке ("Русский", "English", ...).</param>
/// <param name="EnglishName">Название языка на английском (для логов и диагностики).</param>
public sealed record LanguageInfo(string Code, string Name, string EnglishName);

/// <summary>
/// Сервис локализации CoreBus.
/// </summary>
public interface ILocalizationService : INotifyPropertyChanged
{
    IReadOnlyList<LanguageInfo> AvailableLanguages { get; }
    LanguageInfo CurrentLanguage { get; }
    string this[string key] { get; }
    string Get(string key, params object?[] args);
    bool TrySetLanguage(string code);
    event EventHandler? LanguageChanged;
}
