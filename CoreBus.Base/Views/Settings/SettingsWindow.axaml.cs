using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;

namespace CoreBus.Base.Views.Settings;

public partial class SettingsWindow : Window
{
    public static SettingsWindow? Instance { get; private set; }
    public static Border? Workspace { get; private set; }

    public SettingsWindow()
    {
        InitializeComponent();

        Instance = this;
        Workspace = this.FindControl<Border>("Border_Workspace") ?? throw new ArgumentNullException(nameof(Workspace));
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
