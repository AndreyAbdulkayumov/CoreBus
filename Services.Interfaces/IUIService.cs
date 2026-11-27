namespace Services.Interfaces;

public interface IUIService
{
    Task RunInUIThread(Action action);
    Task CopyToClipboard(string data);
    void Set_Dark_Theme();
    void Set_Light_Theme();
    Version? GetAppVersion();
    string? GetAvaloniaVersionString();
    Version GetRuntimeVersion();
}
