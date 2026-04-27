namespace Localization.Avalonia;

public static class Localizer
{
    public static ILocalizationService Instance
    {
        get => SharedLocalizerStore.Instance;
        set => SharedLocalizerStore.Instance = value;
    }

    public static SharedLocValue GetOrCreate(string key)
    {
        return SharedLocalizerStore.GetOrCreate(key);
    }
}
