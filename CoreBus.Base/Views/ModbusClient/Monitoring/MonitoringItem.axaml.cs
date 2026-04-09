using Avalonia.Controls;

namespace CoreBus.Base.Views.ModbusClient.Monitoring;

public partial class MonitoringItem : UserControl
{
    public MonitoringItem()
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