using Avalonia.Controls;

namespace CoreBus.Base.Views.ModbusClient.Manual.ModbusRepresentations;

public partial class Log : UserControl
{
    public Log()
    {
        InitializeComponent();
    }

    private void TextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        var logText = sender as TextBox;

        if (logText != null)
        {
            logText.CaretIndex = 0;
            logText.CaretIndex = logText.Text == null ? 0 : logText.Text.Length;
        }
    }
}
