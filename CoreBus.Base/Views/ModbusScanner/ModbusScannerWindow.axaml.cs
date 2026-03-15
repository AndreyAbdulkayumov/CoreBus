using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace CoreBus.Base.Views.ModbusScanner;

public partial class ModbusScannerWindow : Window
{
    public static ModbusScannerWindow? Instance { get; private set; }

    public ModbusScannerWindow()
    {
        InitializeComponent();

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
