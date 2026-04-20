using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using Services.Interfaces;

namespace CoreBus.Base.Services;

/// <summary>
/// Реализация <see cref="ILocalizationService"/>, загружающая переводы
/// из JSON-файлов в каталоге Localization/ рядом с исполняемым файлом.
///
/// Формат файла (пример — ru.json):
///
///   {
///     "_meta": {
///       "code": "ru",
///       "name": "Русский",
///       "englishName": "Russian"
///     },
///     "Common": {
///       "Close": "Закрыть",
///       "Save": "Сохранить"
///     },
///     "About": {
///       "Developer": "Разработал:"
///     }
///   }
///
/// При разборе файла все вложенные объекты «сплющиваются» в плоскую карту
/// "Common.Close" -> "Закрыть". XAML обращается к ключам по точкам:
///
///   Text="{l:Loc Common.Close}"
///
/// Чтобы добавить новый язык — достаточно положить новый файл вида xx.json
/// в папку Localization. Никаких правок кода или пересборки не требуется:
/// сервис автоматически обнаружит все *.json при старте приложения,
/// а пользователь сможет выбрать новый язык в настройках.
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    private const string LocalizationFolderName = "Localization";
    private const string MetaCodeKey = "_meta.code";
    private const string MetaNameKey = "_meta.name";
    private const string MetaEnglishNameKey = "_meta.englishName";
    private const string FallbackLanguageCode = "en";

    private readonly string _localizationDir;
    private readonly Dictionary<string, Dictionary<string, string>> _translations = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<LanguageInfo> _languages = new();

    private Dictionary<string, string> _current = new(StringComparer.Ordinal);
    private LanguageInfo _currentLang = new(FallbackLanguageCode, "English", "English");

    public IReadOnlyList<LanguageInfo> AvailableLanguages => _languages;

    public LanguageInfo CurrentLanguage => _currentLang;

    public string this[string key] => _current.TryGetValue(key, out var v) ? v : key;

    public event EventHandler? LanguageChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    public LocalizationService()
    {
        _localizationDir = Path.Combine(AppContext.BaseDirectory, LocalizationFolderName);
        LoadAll();
    }

    private void LoadAll()
    {
        _languages.Clear();
        _translations.Clear();

        if (!Directory.Exists(_localizationDir))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(_localizationDir, "*.json"))
        {
            try
            {
                using var stream = File.OpenRead(file);
                using var doc = JsonDocument.Parse(stream);
                var flat = Flatten(doc.RootElement);

                // Код языка берём из _meta.code, иначе — из имени файла (ru.json -> "ru").
                if (!flat.TryGetValue(MetaCodeKey, out var code) || string.IsNullOrWhiteSpace(code))
                {
                    code = Path.GetFileNameWithoutExtension(file);
                }

                var name = flat.TryGetValue(MetaNameKey, out var n) ? n : code;
                var englishName = flat.TryGetValue(MetaEnglishNameKey, out var en) ? en : code;

                _languages.Add(new LanguageInfo(code, name, englishName));
                _translations[code] = flat;
            }
            catch (Exception ex)
            {
                // Битый файл локализации не должен валить приложение.
                // Логируем и идём дальше.
                Console.Error.WriteLine($"[Localization] Cannot load '{file}': {ex.Message}");
            }
        }

        // Упорядочим по названию для стабильного отображения в UI.
        _languages.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));
    }

    public void SetLanguage(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return;
        if (!_translations.TryGetValue(code, out var dict)) return;

        var lang = _languages.FirstOrDefault(l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase));
        if (lang is null) return;

        _current = dict;
        _currentLang = lang;

        // INotifyPropertyChanged с пустой строкой — сигнал «переполните все биндинги».
        // Дополнительно выдаём "Item[]" — стандартное имя для индексаторов в WPF/Avalonia.
        var handler = PropertyChanged;
        handler?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        handler?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public string Get(string key, params object[] args)
    {
        var template = this[key];
        if (args is null || args.Length == 0) return template;
        try
        {
            return string.Format(template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    /// <summary>
    /// Преобразовать вложенный JSON в плоскую карту:
    /// { "a": { "b": "x" } } -> "a.b" = "x".
    /// </summary>
    private static Dictionary<string, string> Flatten(JsonElement root)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        Walk(root, string.Empty, result);
        return result;
    }

    private static void Walk(JsonElement el, string prefix, Dictionary<string, string> acc)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var p in el.EnumerateObject())
                {
                    var name = prefix.Length == 0 ? p.Name : prefix + "." + p.Name;
                    Walk(p.Value, name, acc);
                }
                break;

            case JsonValueKind.String:
                acc[prefix] = el.GetString() ?? string.Empty;
                break;

            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                acc[prefix] = el.ToString();
                break;

            // Массивы и null игнорируем — локализуемые значения всегда строки.
        }
    }
}
