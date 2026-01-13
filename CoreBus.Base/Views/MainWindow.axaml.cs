using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;

namespace CoreBus.Base.Views;

public partial class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }
    public static Grid? Workspace { get; private set; }

    private readonly Border _resizeIcon;

    public MainWindow()
    {
        InitializeComponent();

        Instance = this;
        Workspace = this.FindControl<Grid>("Grid_Workspace") ?? throw new ArgumentNullException(nameof(Workspace));

        _resizeIcon = this.FindControl<Border>("Border_ResizeIcon") ?? throw new ArgumentNullException(nameof(_resizeIcon));
    }

    private void Chrome_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void Button_Minimize_Click(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Button_Maximize_Click(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        _resizeIcon.IsVisible = WindowState == WindowState.Normal;
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
}
