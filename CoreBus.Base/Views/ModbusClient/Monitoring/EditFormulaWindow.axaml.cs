using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ViewModels.ModbusClient.Monitoring;

namespace CoreBus.Base.Views.ModbusClient.Monitoring;

public partial class EditFormulaWindow : Window
{
    public EditFormulaWindow()
    {
        InitializeComponent();
    }

    private void Chrome_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void Button_Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Button_Save_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is EditFormula_VM vm)
        {
            vm.MarkAsSaved();
        }

        Close();
    }
}