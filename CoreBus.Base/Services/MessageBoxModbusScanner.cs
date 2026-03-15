using MessageBox.AvaloniaUI;
using MessageBox.Core;
using Services.Interfaces;
using System;
using System.Threading.Tasks;
using CoreBus.Base.Views.ModbusScanner;

namespace CoreBus.Base.Services;

public class MessageBoxModbusScanner : IMessageBoxModbusScanner
{
    private readonly IUIService _uiService;

    private string? _appVersion;

    public MessageBoxModbusScanner(IUIService uiService)
    {
        _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));

        _appVersion = _uiService.GetAppVersion()?.ToString();
    }

    public void Show(string message, MessageType type, Exception? error = null)
    {
        var messageBox = new MessageBoxManager(ModbusScannerWindow.Instance, _appVersion);

        messageBox.Show(message, type, error);
    }

    public async Task<MessageBoxResult> ShowYesNoDialog(string message, MessageType type, Exception? error = null)
    {
        var messageBox = new MessageBoxManager(ModbusScannerWindow.Instance, _appVersion);

        return await messageBox.ShowYesNoDialog(message, type, error);
    }
}
