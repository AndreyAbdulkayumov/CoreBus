using MessageBox.AvaloniaUI;
using MessageBox.Core;
using Services.Interfaces;
using System;
using System.Threading.Tasks;
using CoreBus.Base.Views.Settings;

namespace CoreBus.Base.Services;

public class MessageBoxSettings : IMessageBoxSettings
{
    private readonly IUIService _uiService;

    private readonly string? _appVersion;

    public MessageBoxSettings(IUIService uiService)
    {
        _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));

        _appVersion = _uiService.GetAppVersion()?.ToString();
    }

    public void Show(string message, MessageType type, Exception? error = null)
    {
        var messageBox = new MessageBoxManager(SettingsWindow.Instance, _appVersion);

        messageBox.Show(message, type, error);
    }

    public async Task<MessageBoxResult> ShowYesNoDialog(string message, MessageType type, Exception? error = null)
    {
        var messageBox = new MessageBoxManager(SettingsWindow.Instance, _appVersion);

        return await messageBox.ShowYesNoDialog(message, type, error);
    }
}
