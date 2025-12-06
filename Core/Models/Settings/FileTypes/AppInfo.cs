namespace Core.Models.Settings.FileTypes;

public enum AppTheme
{
    Dark,
    Light
}

public enum AppMode
{
    NoProtocol,
    ModbusClient
}

public class AppInfo
{
    public string? SelectedPresetFileName { get; set; }
    public AppTheme ThemeName { get; set; }
    public AppMode SelectedMode { get; set; }
    public bool IsModbusMonitoringMode { get; set; }
    public bool CheckUpdateAfterStart { get; set; }
    public string? SkippedAppVersion { get; set; }


    public static AppInfo GetDefault(string defaultPresetName)
    {
        return new AppInfo()
        {
            SelectedPresetFileName = defaultPresetName,
            ThemeName = AppTheme.Dark,
            SelectedMode = AppMode.ModbusClient,
            IsModbusMonitoringMode = false,
            CheckUpdateAfterStart = true,
            SkippedAppVersion = null,
        };
    }
}
