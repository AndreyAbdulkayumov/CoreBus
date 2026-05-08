using CoreBus.Base.Views.Chart;
using MessageBox.Avalonia;
using MessageBox.Core;
using Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace CoreBus.Base.Services;

internal class MessageBoxChart : IMessageBoxChart
{
    private readonly IUIService _uiService;
    private readonly ILocalizationService _localization;

    private readonly string? _appVersion;

    public MessageBoxChart(IUIService uiService, ILocalizationService localization)
    {
        _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));

        _appVersion = _uiService.GetAppVersion()?.ToString();
    }

    public void Show(string message, MessageType type, Exception? error = null)
    {
        var messageBox = new MessageBoxManager(ChartWindow.Instance, _appVersion, _localization);

        messageBox.Show(message, type, error);
    }

    public async Task ShowDialog(string message, MessageType type, Exception? error = null)
    {
        var messageBox = new MessageBoxManager(ChartWindow.Instance, _appVersion, _localization);

        await messageBox.ShowDialog(message, type, error);
    }

    public async Task<MessageBoxResult> ShowYesNoDialog(string message, MessageType type, Exception? error = null)
    {
        var messageBox = new MessageBoxManager(ChartWindow.Instance, _appVersion, _localization);

        return await messageBox.ShowYesNoDialog(message, type, error);
    }
}
