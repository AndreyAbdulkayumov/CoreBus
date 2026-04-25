using System.Threading.Tasks;
using MessageBox.AvaloniaUI;
using MessageBox.Core;
using CoreBus.Base.Views.Macros;
using Services.Interfaces;
using System;

namespace CoreBus.Base.Services;

public class MessageBoxMacros : IMessageBoxMacros
{
    private readonly IUIService _uiService;

    private readonly string? _appVersion;

    public MessageBoxMacros(IUIService uiService)
    {
        _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));

        _appVersion = _uiService.GetAppVersion()?.ToString();
    }

    public void Show(string message, MessageType type, Exception? error = null)
    {
        var messageBox = new MessageBoxManager(MacrosWindow.Instance, _appVersion);

        messageBox.Show(message, type, error);
    }

    public async Task<MessageBoxResult> ShowYesNoDialog(string message, MessageType type, Exception? error = null)
    {
        var messageBox = new MessageBoxManager(MacrosWindow.Instance, _appVersion);

        return await messageBox.ShowYesNoDialog(message, type, error);
    }
}
