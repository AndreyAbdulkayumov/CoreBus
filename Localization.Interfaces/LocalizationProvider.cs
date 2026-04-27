namespace Localization.Interfaces;

/// <summary>
/// Глобальная точка доступа к <see cref="ILocalizationService"/> для кода,
/// в который нельзя удобно прокинуть сервис через DI.
/// </summary>
public static class LocalizationProvider
{
    private static ILocalizationService? _instance;

    public static ILocalizationService Instance
    {
        get => _instance ?? throw new InvalidOperationException(
            "LocalizationProvider.Instance is not initialized. Set it in App startup.");
        set => _instance = value ?? throw new ArgumentNullException(nameof(value));
    }

    public static string Get(string key)
    {
        return _instance is null ? key : _instance[key];
    }

    public static string Get(string key, params object?[] args)
    {
        return _instance is null ? key : _instance.Get(key, args);
    }
}
