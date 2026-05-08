using System;
using System.Threading.Tasks;
using MessageBox.Avalonia;
using MessageBox.Core;
using Services.Interfaces;
using CoreBus.Base.Views.Macros.EditMacros;

namespace CoreBus.Base.Services;

public class MessageBoxEditMacros : IMessageBoxEditMacros
{
    private readonly IUIService _uiService;
    private readonly ILocalizationService _localization;

    private readonly string? _appVersion;

    public MessageBoxEditMacros(IUIService uiService, ILocalizationService localization)
    {
        _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));

        _appVersion = _uiService.GetAppVersion()?.ToString();
    }

    public void Show(string message, MessageType type, Exception? error = null)
    {
        var messageBox = new MessageBoxManager(EditMacrosWindow.Instance, _appVersion, _localization);

        messageBox.Show(message, type, error);
    }

    public async Task ShowDialog(string message, MessageType type, Exception? error = null)
    {
        var messageBox = new MessageBoxManager(EditMacrosWindow.Instance, _appVersion, _localization);

        await messageBox.ShowDialog(message, type, error);
    }

    public async Task<MessageBoxResult> ShowYesNoDialog(string message, MessageType type, Exception? error = null)
    {
        var messageBox = new MessageBoxManager(EditMacrosWindow.Instance, _appVersion, _localization);

        return await messageBox.ShowYesNoDialog(message, type, error);
    }
}
