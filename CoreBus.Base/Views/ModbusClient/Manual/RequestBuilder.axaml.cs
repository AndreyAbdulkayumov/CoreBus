using Avalonia.Controls;

namespace CoreBus.Base.Views.ModbusClient.Manual;

public partial class RequestBuilder : UserControl
{
    public RequestBuilder()
    {
        InitializeComponent();
    }

    private void UppercaseTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        TextBox? textBox = sender as TextBox;

        if (textBox != null)
        {
            textBox.Text = textBox.Text?.ToUpper();
        }
    }
}
