using CoreBus.Base.Views.Chart;
using MessageBox.AvaloniaUI;
using MessageBox.Core;
using Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace CoreBus.Base.Services;

internal class MessageBoxChart : IMessageBoxChart
{
    private readonly IUIService _uiService;

    private string? _appVersion;

    public MessageBoxChart(IUIService uiService)
    {
        _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));

        _appVersion = _uiService.GetAppVersion()?.ToString();
    }

    public void Show(string message, MessageType type, Exception? error = null)
    {
        var messageBox = new MessageBoxManager(ChartWindow.Instance, _appVersion);

        messageBox.Show(message, type, error);
    }

    public async Task<MessageBoxResult> ShowYesNoDialog(string message, MessageType type, Exception? error = null)
    {
        var messageBox = new MessageBoxManager(ChartWindow.Instance, _appVersion);

        return await messageBox.ShowYesNoDialog(message, type, error);
    }
}
