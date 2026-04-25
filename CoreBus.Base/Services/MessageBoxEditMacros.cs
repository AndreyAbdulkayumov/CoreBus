using System;
using System.Threading.Tasks;
using MessageBox.AvaloniaUI;
using MessageBox.Core;
using Services.Interfaces;
using CoreBus.Base.Views.Macros.EditMacros;

namespace CoreBus.Base.Services;

public class MessageBoxEditMacros : IMessageBoxEditMacros
{
    private readonly IUIService _uiService;

    private readonly string? _appVersion;

    public MessageBoxEditMacros(IUIService uiService)
    {
        _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));

        _appVersion = _uiService.GetAppVersion()?.ToString();
    }

    public void Show(string message, MessageType type, Exception? error = null)
    {
        var messageBox = new MessageBoxManager(EditMacrosWindow.Instance, _appVersion);

        messageBox.Show(message, type, error);
    }

    public async Task<MessageBoxResult> ShowYesNoDialog(string message, MessageType type, Exception? error = null)
    {
        var messageBox = new MessageBoxManager(EditMacrosWindow.Instance, _appVersion);

        return await messageBox.ShowYesNoDialog(message, type, error);
    }
}
