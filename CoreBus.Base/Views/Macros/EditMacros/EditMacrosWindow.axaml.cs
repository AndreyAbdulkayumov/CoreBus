using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;

namespace CoreBus.Base.Views.Macros.EditMacros;

public partial class EditMacrosWindow : Window
{
    public static EditMacrosWindow? Instance { get; private set; }

    public EditMacrosWindow()
    {
        InitializeComponent();

        // Чтобы убрать белую рамку вокруг окна на Linux (проблема появилась с миграцией на Avalonia 12.1)
        if (OperatingSystem.IsLinux())
        {
            WindowDecorations = WindowDecorations.None;
        }

        Instance = this;
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