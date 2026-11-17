using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;

namespace MessageBox.Avalonia.Views;

public partial class ViewErrorWindow : Window
{
    private readonly ILocalizationService _localization;

    public ViewErrorWindow(ILocalizationService localization)
    {
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));

        InitializeComponent();

        // Чтобы убрать белую рамку вокруг окна на Linux (проблема появилась с миграцией на Avalonia 12.1)
        if (OperatingSystem.IsLinux())
        {
            WindowDecorations = WindowDecorations.None;
        }

        UpdateLocalization();

        _localization.LanguageChanged += LocalizationOnLanguageChanged;
    }

    private void LocalizationOnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateLocalization();
    }

    public void SetErrorReport(string errorReport)
    {
        TextBlock_ErrorReport.Text = errorReport;
    }

    private void Window_OnClosed(object? sender, EventArgs e)
    {
        _localization.LanguageChanged -= LocalizationOnLanguageChanged;
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

    private void ResizeIcon_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Cursor = new(StandardCursorType.BottomRightCorner);
        BeginResizeDrag(WindowEdge.SouthEast, e);
        Cursor = new(StandardCursorType.Arrow);
    }

    private void UpdateLocalization()
    {
        Title = _localization.Get("Dialog.MessageBox.ErrorReport");
        ToolTip.SetTip(Button_Close, _localization.Get("Common.Close"));
    }
}
