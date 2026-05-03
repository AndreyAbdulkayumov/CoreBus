using System.ComponentModel;
using System.Text.Json;

namespace Localization.Core;

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
    public string this[string key] => _current.TryGetValue(key, out var value) ? value : key;

    public event EventHandler? LanguageChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    public LocalizationService()
    {
        _localizationDir = Path.Combine(AppContext.BaseDirectory, LocalizationFolderName);
        LoadAll();
    }

    public bool TrySetLanguage(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || !_translations.TryGetValue(code, out var dict))
        {
            return false;
        }

        var lang = _languages.FirstOrDefault(l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase));
        if (lang is null)
        {
            return false;
        }

        _current = dict;
        _currentLang = lang;

        var handler = PropertyChanged;
        handler?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        handler?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        LanguageChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public string Get(string key, params object?[] args)
    {
        var template = this[key];
        if (args is null || args.Length == 0)
        {
            return template;
        }

        try
        {
            return string.Format(template, args);
        }
        catch (FormatException)
        {
            return template;
        }
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

                if (!flat.TryGetValue(MetaCodeKey, out var code) || string.IsNullOrWhiteSpace(code))
                {
                    code = Path.GetFileNameWithoutExtension(file);
                }

                var name = flat.TryGetValue(MetaNameKey, out var localizedName) ? localizedName : code;
                var englishName = flat.TryGetValue(MetaEnglishNameKey, out var enName) ? enName : code;

                _languages.Add(new LanguageInfo(code, name, englishName));
                _translations[code] = flat;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Localization] Cannot load '{file}': {ex.Message}");
            }
        }

        _languages.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));

        var index = _languages.FindIndex(l => string.Equals(l.Code, "ru", StringComparison.OrdinalIgnoreCase));

        if (index > 0)
        {
            var ru = _languages[index];
            _languages.RemoveAt(index);
            _languages.Insert(0, ru);
        }
    }

    private static Dictionary<string, string> Flatten(JsonElement root)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        Walk(root, string.Empty, result);
        return result;
    }

    private static void Walk(JsonElement element, string prefix, Dictionary<string, string> acc)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var name = prefix.Length == 0 ? property.Name : prefix + "." + property.Name;
                    Walk(property.Value, name, acc);
                }
                break;
            case JsonValueKind.String:
                acc[prefix] = element.GetString() ?? string.Empty;
                break;
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                acc[prefix] = element.ToString();
                break;
        }
    }
}
