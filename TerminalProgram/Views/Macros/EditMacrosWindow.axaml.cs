using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MessageBox_AvaloniaUI;
using System.Collections.Generic;
using ViewModels.Macros;

namespace TerminalProgram.Views.Macros;

public partial class EditMacrosWindow : Window
{
    private readonly EditMacros_VM _viewModel;

    public EditMacrosWindow(IEnumerable<string?>? existingMacrosNames)
    {
        InitializeComponent();

        _viewModel = new EditMacros_VM(
            existingMacrosNames, 
            Close, 
            new MessageBox(this, "�������")
            );

        DataContext = _viewModel;
    }

    public object? GetData()
    {
        return _viewModel.Saved ? _viewModel.GetMacrosContent() : null;
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