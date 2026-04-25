using System;

namespace Services.Interfaces;

/// <summary>
/// Глобальная точка доступа к <see cref="ILocalizationService"/> для кода,
/// в который нельзя удобно прокинуть сервис через DI — например, static-классы-хелперы,
/// исключения в ViewModels, абстрактные базовые классы без конструктора.
///
/// Заполняется один раз при старте приложения (в App.axaml.cs) после того,
/// как построен DI-контейнер. Для ViewModels предпочтительнее получать
/// <see cref="ILocalizationService"/> через конструктор, а этим провайдером
/// пользоваться только там, где это невозможно.
/// </summary>
public static class LocalizationProvider
{
    private static ILocalizationService? _instance;

    /// <summary>
    /// Текущий экземпляр сервиса локализации.
    /// Выбрасывает <see cref="InvalidOperationException"/>, если не инициализирован.
    /// </summary>
    public static ILocalizationService Instance
    {
        get => _instance ?? throw new InvalidOperationException(
            "LocalizationProvider.Instance is not initialized. Set it in App startup.");
        set => _instance = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Безопасно получить локализованную строку по ключу.
    /// Если провайдер ещё не инициализирован — возвращает сам ключ.
    /// </summary>
    public static string Get(string key)
    {
        return _instance is null ? key : _instance[key];
    }

    /// <summary>
    /// То же, что <see cref="Get(string)"/>, но с форматированием.
    /// </summary>
    public static string Get(string key, params object[] args)
    {
        return _instance is null ? key : _instance.Get(key, args);
    }
}
