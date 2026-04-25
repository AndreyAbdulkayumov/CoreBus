using MessageBox.AvaloniaUI;
using MessageBox.Core;
using System.Threading.Tasks;
using CoreBus.Base.Views;
using Services.Interfaces;
using System;

namespace CoreBus.Base.Services;

public class MessageBoxMainWindow : IMessageBoxMainWindow
{
    private readonly IUIService _uiService;

    private readonly string? _appVersion;

    public MessageBoxMainWindow(IUIService uiService)
    {
        _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));

        _appVersion = _uiService.GetAppVersion()?.ToString();
    }

    public void Show(string message, MessageType type, Exception? error = null)
    {
        var messageBox = new MessageBoxManager(MainWindow.Instance, _appVersion);

        messageBox.Show(message, type, error);
    }

    public async Task<MessageBoxResult> ShowYesNoDialog(string message, MessageType type, Exception? error = null)
    {
        var messageBox = new MessageBoxManager(MainWindow.Instance, _appVersion);

        return await messageBox.ShowYesNoDialog(message, type, error);
    }
}
