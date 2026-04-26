using Services.Interfaces;
using System.ComponentModel;

namespace Core.Tests.Infrastructure;

internal sealed class TestLocalizationService : ILocalizationService
{
    private static readonly LanguageInfo DefaultLanguage = new("en", "English", "English");

    public IReadOnlyList<LanguageInfo> AvailableLanguages { get; } = new[] { DefaultLanguage };
    public LanguageInfo CurrentLanguage => DefaultLanguage;
    public string this[string key] => key;

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add { }
        remove { }
    }

    public event EventHandler? LanguageChanged
    {
        add { }
        remove { }
    }

    public string Get(string key, params object[] args)
    {
        if (args == null || args.Length == 0)
        {
            return key;
        }

        return $"{key}|{string.Join(",", args)}";
    }

    public void SetLanguage(string code)
    {
    }
}
