using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MessageBox.Avalonia.ViewModels;
using MessageBox.Core;
using Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MessageBox.Avalonia.Views;

public partial class MessageBoxWindow : Window
{
    public MessageBoxResult Result { get; private set; } = MessageBoxResult.Default;
    private ILocalizationService? _localization;

    public MessageBoxWindow()
    {
        InitializeComponent();
    }

    public void SetDataContext(string message, string title, MessageType messageType, MessageBoxToolType toolType, string? appVersion, ILocalizationService localization, Exception? error = null)
    {
        _localization = localization;

        DataContext = new MessageBox_VM(
            OpenErrorReport, CopyToClipboard, GetFolderPath,
            message, title, messageType, toolType, appVersion, localization, error);
    }

    private void OpenErrorReport(string errorReport)
    {
        if (_localization == null)
            return;

        var window = new ViewErrorWindow(_localization);

        window.SetErrorReport(errorReport);

        window.Show(this);
    }

    private async Task CopyToClipboard(string data)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

        if (clipboard != null)
        {
            await clipboard.SetTextAsync(data);
        }
    }

    public async Task<string?> GetFolderPath(string windowTitle)
    {
        // Get top level from the current control. Alternatively, you can use Window reference instead.
        TopLevel? topLevel = TopLevel.GetTopLevel(this);

        if (topLevel != null)
        {
            var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = windowTitle,
                AllowMultiple = false
            });

            if (folder != null && folder.Count > 0)
            {
                return folder.First().TryGetLocalPath();
            }
        }

        return null;
    }

    private void Button_Click(object? sender, RoutedEventArgs e)
    {
        var clickedButton = sender as Button;

        if (clickedButton?.DataContext is ButtonContent buttonContent)
        {
            Result = buttonContent.Result;

            Close();
        }
    }

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape ||
            e.Key == Key.Enter)
        {
            Close();
        }
    }

    private void Chrome_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void Button_Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
