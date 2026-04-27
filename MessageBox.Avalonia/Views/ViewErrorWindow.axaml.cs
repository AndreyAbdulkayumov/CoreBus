using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Services.Interfaces;
using System;

namespace MessageBox.Avalonia;

public partial class ViewErrorWindow : Window
{
    private readonly ILocalizationService? _localization;

    public ViewErrorWindow()
    {
        InitializeComponent();
    }

    public ViewErrorWindow(ILocalizationService localization)
    {
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));

        InitializeComponent();

        UpdateLocalization();

        _localization.LanguageChanged += (_, _) => UpdateLocalization();
    }

    public void SetErrorReport(string errorReport)
    {
        TextBlock_ErrorReport.Text = errorReport;
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
        if (_localization == null)
            return;

        Title = _localization.Get("MessageBox.ErrorReport");
        ToolTip.SetTip(Button_Close, _localization.Get("MessageBox.Close"));
    }
}