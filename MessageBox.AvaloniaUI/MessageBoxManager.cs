using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using MessageBox.AvaloniaUI.Localization;
using MessageBox.AvaloniaUI.ViewModels;
using MessageBox.AvaloniaUI.Views;
using MessageBox.Core;
using Services.Interfaces;

namespace MessageBox.AvaloniaUI;

public class MessageBoxManager : IMessageBox
{
    private readonly Window? Owner;

    private const string Title = "CoreBus";

    private readonly string? _appVersion;

    private readonly ILocalizationService _localization;

    public MessageBoxManager(Window? owner, string? appVersion, ILocalizationService localization)
    {
        Owner = owner;
        _appVersion = appVersion;

        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        Localizer.Instance = _localization;
    }

    public void Show(string message, MessageType messageType, Exception? error = null)
    {
        Dispatcher.UIThread.Invoke(async () =>
        {
            var window = new MessageBoxWindow();

            window.SetDataContext(message, Title, messageType, MessageBoxToolType.Default, _appVersion, _localization, error);

            await CallMessageBox(window);
        });
    }

    public async Task<MessageBoxResult> ShowYesNoDialog(string message, MessageType messageType, Exception? error = null)
    {
        return await Dispatcher.UIThread.Invoke(async () =>
        {
            var window = new MessageBoxWindow();

            window.SetDataContext(message, Title, messageType, MessageBoxToolType.YesNo, _appVersion, _localization, error);

            await CallMessageBox(window);

            return window.Result;
        });
    }

    private async Task CallMessageBox(Window window)
    {
        if (Owner != null && Owner.IsVisible)
        {
            await window.ShowDialog(Owner);
        }

        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Show();
        }
    }
}
