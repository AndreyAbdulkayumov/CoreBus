using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;

namespace CoreBus.Base.Views;

public partial class ServiceWindow : Window
{
    public string? SelectedFilePath { get; private set; }

    public ServiceWindow()
    {
        InitializeComponent();

        // Чтобы убрать белую рамку вокруг окна на Linux (проблема появилась с миграцией на Avalonia 12.1)
        if (OperatingSystem.IsLinux())
        {
            WindowDecorations = WindowDecorations.None;
        }

        TextBlock_Description.Text = LocalizationProvider.Get("Common.EnterFileName");
    }

    private void Chrome_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void Button_Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Button_Select_Click(object? sender, RoutedEventArgs e)
    {
        SelectedFilePath = TextBox_SelectFileName.Text;
        Close();
    }
}
