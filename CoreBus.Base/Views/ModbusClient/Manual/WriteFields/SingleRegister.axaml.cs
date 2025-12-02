using Avalonia.Controls;

namespace CoreBus.Base.Views.ModbusClient.Manual.WriteFields;

public partial class SingleRegister : UserControl
{
    public SingleRegister()
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